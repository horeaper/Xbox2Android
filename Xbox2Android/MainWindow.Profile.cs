using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Xbox2Android.Input;

namespace Xbox2Android
{
	partial class MainWindow
	{
		const string SettingFileName = "InputSettings.xml";
		readonly List<TouchProfile> m_profiles = new List<TouchProfile>();
		int m_selectedProfileIndex = -1;

		public TouchProfile CurrentProfile => m_profiles[m_selectedProfileIndex];

		void LoadSettings()
		{
			var document = XDocument.Load(SettingFileName);
			var rootElement = document.Root;
			if (rootElement.Name == "Settings") {
				var profile = new TouchProfile();
				if (profile.Load(rootElement)) {
					profile.AssignIfNoName(m_profiles.Count + 1);
					AddNewProfile(profile);
				}
			}
			else if (rootElement.Name == "Profiles") {
				foreach (var element in rootElement.Elements("Settings")) {
					var profile = new TouchProfile();
					if (profile.Load(element)) {
						profile.AssignIfNoName(m_profiles.Count + 1);
						AddNewProfile(profile);
					}
				}
			}

			if (m_profiles.Count > 0) {
				m_selectedProfileIndex = 0;
				var selectedItem = (MenuItem)menuTouchProfiles.Items[m_selectedProfileIndex];
				selectedItem.IsChecked = true;
				OnTouchProfileSelected((TouchProfile)selectedItem.Tag);
			}
			else {
				OnTouchProfileSelected(null);
			}
		}

		bool SaveSettings()
		{
			var rootElement = new XElement("Profiles");
			foreach (var profile in m_profiles) {
				var element = profile.Save();
				if (element == null) {
					return false;
				}
				rootElement.Add(element);
			}
			rootElement.Save(SettingFileName);
			return true;
		}

		void AddNewProfile(TouchProfile profile)
		{
			var menuItem = new MenuItem {
				Header = profile.Name,
				Tag = profile
			};
			menuItem.Click += MenuProfileItem_OnClick;
			menuTouchProfiles.Items.Insert(m_profiles.Count, menuItem);
			m_profiles.Add(profile);
		}

		void MenuProfileItem_OnClick(object sender, RoutedEventArgs e)
		{
			var selectedItem = (MenuItem)sender;
			var profile = (TouchProfile)selectedItem.Tag;
			SelectProfileByIndex(m_profiles.IndexOf(profile));
		}

		void SelectProfileByIndex(int index)
		{
			Debug.Assert(index >= 0 && index < m_profiles.Count);
			m_selectedProfileIndex = index;
			for (int cnt = 0; cnt < m_profiles.Count; ++cnt) {
				((MenuItem)menuTouchProfiles.Items[cnt]).IsChecked = false;
			}
			((MenuItem)menuTouchProfiles.Items[index]).IsChecked = true;
			OnTouchProfileSelected(m_profiles[index]);
		}

		void MenuNewProfile_OnClick(object sender, RoutedEventArgs e)
		{
			var inputWindow = new TextInputWindow(this, "New Profile");
			if (inputWindow.ShowDialog() == true) {
				var profile = new TouchProfile { Name = inputWindow.Result };
				AddNewProfile(profile);
				new TouchProfileWindow(profile).ShowDialog();
			}
		}

		void MenuEditProfile_OnClick(object sender, RoutedEventArgs e)
		{
			if (m_selectedProfileIndex != -1) {
				new TouchProfileWindow(CurrentProfile).ShowDialog();
			}
		}

		void MenuRenameProfile_OnClick(object sender, RoutedEventArgs e)
		{
			if (m_selectedProfileIndex != -1) {
				var profile = CurrentProfile;
				var inputWindow = new TextInputWindow(this, "Rename Profile", profile.Name);
				if (inputWindow.ShowDialog() == true) {
					profile.Name = inputWindow.Result;
					foreach (MenuItem item in menuTouchProfiles.Items) {
						if (item.Tag == profile) {
							item.Header = profile.Name;
							break;
						}
					}
				}
			}
		}

		void MenuDeleteProfile_OnClick(object sender, RoutedEventArgs e)
		{
			if (m_selectedProfileIndex != -1) {
				if (MessageBox.Show(this, "Delete current selected profile?", "Delete Profile", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
					menuTouchProfiles.Items.RemoveAt(m_selectedProfileIndex);
					m_profiles.RemoveAt(m_selectedProfileIndex);

					if (m_profiles.Count > 0) {
						m_selectedProfileIndex %= m_profiles.Count;
						for (int cnt = 0; cnt < m_profiles.Count; ++cnt) {
							((MenuItem)menuTouchProfiles.Items[cnt]).IsChecked = false;
						}
						var selectedItem = (MenuItem)menuTouchProfiles.Items[m_selectedProfileIndex];
						selectedItem.IsChecked = true;
						OnTouchProfileSelected((TouchProfile)selectedItem.Tag);
					}
					else {
						m_selectedProfileIndex = -1;
						OnTouchProfileSelected(null);
					}
				}
			}
		}

		void OnTouchProfileSelected(TouchProfile profile)
		{
			if (profile != null) {
				triggerModeHappy.IsEnabled = true;
				triggerModeDouble.IsEnabled = true;
				triggerModeTriple.IsEnabled = true;
				checkHotKey.IsEnabled = true;
				checkReverseAxis.IsEnabled = true;
				checkSnapAxis.IsEnabled = true;

				IsLoading = true;
				triggerModeHappy.Value = profile.TriggerHappyValue;
				triggerModeDouble.Value = profile.TriggerDoubleValue;
				triggerModeTriple.Value = profile.TriggerTripleValue;
				triggerModeHappy.IsChecked = false;
				triggerModeDouble.IsChecked = false;
				triggerModeTriple.IsChecked = false;
				switch (profile.TriggerMode) {
					case 0:
						triggerModeHappy.IsChecked = true;
						break;
					case 1:
						triggerModeDouble.IsChecked = true;
						break;
					case 2:
						triggerModeTriple.IsChecked = true;
						break;
				}
				checkHotKey.IsChecked = profile.IsHotKey;
				checkReverseAxis.IsChecked = profile.IsReverseAxis;
				checkSnapAxis.IsChecked = profile.IsSnapAxis;
				IsLoading = false;
			}
			else {
				triggerModeHappy.IsEnabled = false;
				triggerModeDouble.IsEnabled = false;
				triggerModeTriple.IsEnabled = false;
				checkHotKey.IsEnabled = false;
				checkReverseAxis.IsEnabled = false;
				checkSnapAxis.IsEnabled = false;
			}

			InputMapper.Profile = profile;
		}
	}
}
