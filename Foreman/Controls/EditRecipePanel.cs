﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Foreman
{
	public partial class EditRecipePanel : UserControl
	{
		private ProductionGraphViewer myGraphViewer;
		private BaseNode baseNode;

		public EditRecipePanel(BaseNode baseNode, ProductionGraphViewer graphViewer)
		{
			InitializeComponent();
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);


			this.baseNode = baseNode;
			myGraphViewer = graphViewer;
			RateGroup.Text = string.Format("Item Flowrate (per {0})", myGraphViewer.GetRateName());
			fixedTextBox.Text = Convert.ToString(baseNode.DesiredRate);

			if (this.baseNode.RateType == RateType.Auto)
			{
				autoOption.Checked = true;
				fixedTextBox.Enabled = false;
			}
			else
			{
				fixedOption.Checked = true;
				fixedTextBox.Enabled = true;
			}

			if (baseNode is RecipeNode recipeNode) //check for valid assembler count is not necessary (data cache deletes any recipes with 0 assemblers), but just in case
			{
				RateGroup.Text = "Number of assemblers:";
				fixedTextBox.Text = Convert.ToString(recipeNode.GetBaseNumberOfAssemblers() * myGraphViewer.GetRateMultipler());

				updateInProgress = true;
				Recipe recipe = recipeNode.BaseRecipe;

				AssemblerSelectionBox.Items.AddRange(recipe.Assemblers.Where(a => a.Enabled).ToArray());
				if (recipeNode.SelectedAssembler != null && !AssemblerSelectionBox.Items.Contains(recipeNode.SelectedAssembler))
					AssemblerSelectionBox.Items.Add(recipeNode.SelectedAssembler);

				AssemblerSelectionBox.SelectedItem = recipeNode.SelectedAssembler ?? AssemblerSelectionBox.Items[0] ?? null;

				if (recipeNode.AssemblerModules.Count > 0)
				{
					AModuleSelectionBox.Items.Add(recipeNode.AssemblerModules[0]);
					AModuleSelectionBox.SelectedItem = recipeNode.AssemblerModules[0];
				}
				if (recipeNode.Fuel != null)
				{
					AFuelSelectionBox.Items.Add(recipeNode.Fuel);
					AFuelSelectionBox.SelectedItem = recipeNode.Fuel;
				}
				if (recipeNode.SelectedBeacon != null)
				{
					BeaconSelectionBox.Items.Add(recipeNode.SelectedBeacon);
					BeaconSelectionBox.SelectedItem = recipeNode.SelectedBeacon;
				}
				if (recipeNode.BeaconModules.Count > 0)
				{
					BModuleSelectionBox.Items.Add(recipeNode.BeaconModules[0]);
					BModuleSelectionBox.SelectedItem = recipeNode.BeaconModules[0];
				}
				BeaconCounter.Value = (decimal)recipeNode.BeaconCount;

				updateInProgress = false;
				UpdateOptions();
			}
			else
			{
				this.AssemblerGroup.Visible = false;
				this.BeaconGroup.Visible = false;
			}
		}

		private bool updateInProgress = false; //used to ensure UpdateOptions isnt called forever by it changing selection box values / selected indices
		private void UpdateOptions()
		{
			if (updateInProgress)
				return;

			if (baseNode is RecipeNode recipeNode)
			{
				Recipe recipe = recipeNode.BaseRecipe;

				Assembler currentAssembler = AssemblerSelectionBox.SelectedItem as Assembler;
				Module currentAModule = AModuleSelectionBox.SelectedItem as Module;
				Item currentFuel = AFuelSelectionBox.SelectedItem as Item;
				Beacon currentBeacon = BeaconSelectionBox.SelectedItem as Beacon;
				Module currentBModule = BModuleSelectionBox.SelectedItem as Module;

				if (currentAssembler == null || !currentAssembler.Enabled)
					return;
				updateInProgress = true;

				AModuleSelectionBox.Items.Clear();
				AFuelSelectionBox.Items.Clear();
				BeaconSelectionBox.Items.Clear();
				BModuleSelectionBox.Items.Clear();

				//update the assembler
				recipeNode.SetAssembler(currentAssembler);

				//fill in the module options
				AModuleSelectionBox.Enabled = (currentAssembler.ModuleSlots > 0);
				if (AModuleSelectionBox.Enabled)
				{
					AModuleSelectionBox.Items.Add("No Modules");
					AModuleSelectionBox.Items.AddRange(currentAssembler.Modules.Intersect(recipe.Modules).ToArray());
					AModuleSelectionBox.Enabled = AModuleSelectionBox.Items.Count > 0;
					if (currentAModule != null && AModuleSelectionBox.Items.Contains(currentAModule))
						AModuleSelectionBox.SelectedItem = currentAModule;
					else if (AModuleSelectionBox.Items.Count > 0)
						AModuleSelectionBox.SelectedIndex = 0;

					currentAModule = AModuleSelectionBox.SelectedItem as Module;
				}
				recipeNode.SetAssemblerModules(null);
				if (currentAModule != null)
					for (int i = 0; i < currentAssembler.ModuleSlots; i++)
						recipeNode.AddAssemblerModule(currentAModule);

				//fuel
				AFuelSelectionBox.Enabled = currentAssembler.IsBurner;
				if (AFuelSelectionBox.Enabled)
				{
					AFuelSelectionBox.Items.AddRange(currentAssembler.Fuels.ToArray());
					if (currentFuel != null && AFuelSelectionBox.Items.Contains(currentFuel))
						AFuelSelectionBox.SelectedItem = currentFuel;
					else if (AFuelSelectionBox.Items.Count > 0)
					{
						recipeNode.AutoSetFuel();
						AFuelSelectionBox.SelectedIndex = AFuelSelectionBox.Items.IndexOf(recipeNode.Fuel);
					}

					currentFuel = AFuelSelectionBox.SelectedItem as Item;
				}
				if (!currentAssembler.IsBurner) currentFuel = null; //quick check to ensure that if we switch from a burner to a non-burner then the fuel is set to null
				recipeNode.SetFuel(currentFuel);


				//beacons
				BeaconGroup.Enabled = currentAssembler.ModuleSlots > 0;
				BeaconCounter.Enabled = BeaconGroup.Enabled;
				if (BeaconGroup.Enabled)
				{
					BeaconSelectionBox.Items.Add("No Beacons");
					BeaconSelectionBox.Items.AddRange(myGraphViewer.DCache.Beacons.Values.ToArray());
					if (currentBeacon != null && BeaconSelectionBox.Items.Contains(currentBeacon))
						BeaconSelectionBox.SelectedItem = currentBeacon;
					else if (BeaconSelectionBox.Items.Count > 0)
						BeaconSelectionBox.SelectedIndex = 0;

					currentBeacon = BeaconSelectionBox.SelectedItem as Beacon;
					BeaconCounter.Enabled = (BeaconSelectionBox.Items.Count > 0);
				}
				recipeNode.SetBeacon(currentBeacon);


				//beacon modules
				BModuleSelectionBox.Enabled = (currentBeacon != null && currentBeacon.ModuleSlots > 0);
				if (BModuleSelectionBox.Enabled)
				{
					BModuleSelectionBox.Items.Add("No Modules");
					BModuleSelectionBox.Items.AddRange(currentBeacon.ValidModules.Intersect(recipe.Modules).ToArray());
					BModuleSelectionBox.Enabled = BModuleSelectionBox.Items.Count > 0;
					if (currentBModule != null && BModuleSelectionBox.Items.Contains(currentBModule))
						BModuleSelectionBox.SelectedItem = currentBModule;
					else if (BModuleSelectionBox.Items.Count > 0)
						BModuleSelectionBox.SelectedIndex = 0;

					currentBModule = BModuleSelectionBox.SelectedItem as Module;
				}
				recipeNode.SetBeaconModules(null);
				if (currentBModule != null)
					for (int i = 0; i < currentBeacon.ModuleSlots; i++)
						recipeNode.AddBeaconModule((currentBModule));

				myGraphViewer.Graph.UpdateNodeValues();
				updateInProgress = false;
			}

			if (baseNode is RecipeNode rNode)
				fixedTextBox.Text = Convert.ToString(rNode.GetBaseNumberOfAssemblers() * myGraphViewer.GetRateMultipler());
			else
				fixedTextBox.Text = Convert.ToString(baseNode.DesiredRate);
		}

		private void SetFixedRate()
		{
			if (float.TryParse(fixedTextBox.Text, out float newAmount))
			{
				if (baseNode is RecipeNode rNode)
					rNode.SetBaseNumberOfAssemblers(newAmount / myGraphViewer.GetRateMultipler());
				else
					baseNode.DesiredRate = newAmount;

				myGraphViewer.Graph.UpdateNodeValues();
			}
		}

		private void fixedOption_CheckedChanged(object sender, EventArgs e)
		{
			fixedTextBox.Enabled = fixedOption.Checked;
			RateType updatedRateType = (fixedOption.Checked) ? RateType.Manual : RateType.Auto;

			if (baseNode.RateType != updatedRateType)
			{
				baseNode.RateType = updatedRateType;
				myGraphViewer.Graph.UpdateNodeValues();
			}
		}

		private void fixedTextBox_LostFocus(object sender, EventArgs e)
		{
			SetFixedRate();
		}

		private void KeyPressed(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				SetFixedRate();
		}

		//------------------------------------------------------------------------------------------assembler panel events

		private void AssemblerSelectionBox_SelectedIndexChanged(object sender, EventArgs e) { UpdateOptions(); }
		private void AModuleSelectionBox_SelectedIndexChanged(object sender, EventArgs e) { UpdateOptions(); }
		private void AFuelSelectionBox_SelectedIndexChanged(object sender, EventArgs e) { UpdateOptions(); }

		//------------------------------------------------------------------------------------------beacon panel events

		private void BeaconSelectionBox_SelectedIndexChanged(object sender, EventArgs e) { UpdateOptions(); }
		private void BModuleSelectionBox_SelectedIndexChanged(object sender, EventArgs e) { UpdateOptions(); }
		private void BeaconCounter_ValueChanged(object sender, EventArgs e)
		{
			if (baseNode is RecipeNode recipeNode)
			{
				recipeNode.BeaconCount = (float)BeaconCounter.Value;
				UpdateOptions();
			}
		}
	}
}
