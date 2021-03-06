﻿#region

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Windows;
using MahApps.Metro.Controls.Dialogs;

#endregion

namespace Hearthstone_Deck_Tracker
{
	public partial class MainWindow
	{
		private void BtnExport_Click(object sender, RoutedEventArgs e)
		{
			var deck = DeckList.Instance.ActiveDeck;
			if(deck == null)
				return;
			ExportDeck(deck.GetSelectedDeckVersion());
		}

		private async void ExportDeck(Deck deck)
		{
			var message =
				string.Format(
				              "1) create a new, empty {0}-Deck {1}.\n\n2) leave the deck creation screen open.\n\n3)do not move your mouse or type after clicking \"export\"",
				              deck.Class, (Config.Instance.AutoClearDeck ? "(or open an existing one to be cleared automatically)" : ""));

			if(deck.GetSelectedDeckVersion().Cards.Any(c => c.Name == "Stalagg" || c.Name == "Feugen"))
			{
				message +=
					"\n\nIMPORTANT: If you own golden versions of Feugen or Stalagg please make sure to configure\nOptions > Other > Exporting";
			}

			var settings = new MetroDialogSettings {AffirmativeButtonText = "export"};
			var result =
				await
				this.ShowMessageAsync("Export " + deck.Name + " to Hearthstone", message, MessageDialogStyle.AffirmativeAndNegative, settings);

			if(result == MessageDialogResult.Affirmative)
			{
				var controller = await this.ShowProgressAsync("Creating Deck", "Please do not move your mouse or type.");
				Topmost = false;
				await Task.Delay(500);
				await DeckExporter.Export(deck);
				await controller.CloseAsync();

				if(deck.MissingCards.Any())
					this.ShowMissingCardsMessage(deck);
			}
		}


		private async void BtnScreenhot_Click(object sender, RoutedEventArgs e)
		{
			if(DeckList.Instance.ActiveDeck == null)
				return;
			Logger.WriteLine("Creating screenshot of " + DeckList.Instance.ActiveDeckVersion.GetDeckInfo(), "Screenshot");
			var screenShotWindow = new PlayerWindow(Config.Instance, DeckList.Instance.ActiveDeckVersion.Cards, true);
			screenShotWindow.Show();
			screenShotWindow.Top = 0;
			screenShotWindow.Left = 0;
			await Task.Delay(100);
			var source = PresentationSource.FromVisual(screenShotWindow);
			if(source == null)
				return;

			var dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
			var dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;

			var deck = DeckList.Instance.ActiveDeckVersion;
			var pngEncoder = Helper.ScreenshotDeck(screenShotWindow.ListViewPlayer, dpiX, dpiY, deck.Name);
			screenShotWindow.Shutdown();

			if(pngEncoder != null)
			{
				var fileName = Helper.ShowSaveFileDialog(deck.Name, "png");

				if(fileName != null)
				{
					using(var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
						pngEncoder.Save(stream);

					await this.ShowSavedFileMessage(fileName);
					Logger.WriteLine("Saved screenshot of " + deck.GetDeckInfo() + " to file: " + fileName, "Export");
				}
			}
		}

		private async void BtnSaveToFile_OnClick(object sender, RoutedEventArgs e)
		{
			var deck = DeckList.Instance.ActiveDeckVersion;
			if(deck == null)
				return;

			var fileName = Helper.ShowSaveFileDialog(deck.Name, "xml");

			if(fileName != null)
			{
				XmlManager<Deck>.Save(fileName, deck);
				await this.ShowSavedFileMessage(fileName);
				Logger.WriteLine("Saved " + deck.GetDeckInfo() + " to file: " + fileName, "Export");
			}
		}

		private void BtnClipboard_OnClick(object sender, RoutedEventArgs e)
		{
			var deck = DeckList.Instance.ActiveDeckVersion;
			if(deck == null)
				return;
			Clipboard.SetText(Helper.DeckToIdString(deck));
			this.ShowMessage("", "copied to clipboard");
			Logger.WriteLine("Copied " + deck.GetDeckInfo() + " to clipboard", "Export");
		}

		private async void BtnExportFromWeb_Click(object sender, RoutedEventArgs e)
		{
			var url = await InputDeckURL();
			if(url == null)
				return;

			var deck = await ImportDeckFromURL(url);

			if(deck != null)
				ExportDeck(deck);
			else
				await this.ShowMessageAsync("Error", "Could not load deck from specified url");
		}

		internal void MenuItemMissingDust_OnClick(object sender, RoutedEventArgs e)
		{
			var deck = DeckList.Instance.ActiveDeckVersion;
			if(deck == null)
				return;
			this.ShowMissingCardsMessage(deck);
		}
	}
}