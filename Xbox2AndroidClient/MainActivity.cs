using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace Xbox2AndroidClient
{
	[Activity(Label = "Xbox2Android", MainLauncher = true, Icon = "@drawable/ic_launcher")]
	public partial class MainActivity : Activity
	{
		EditText textName;
		EditText textIP;
		TextView labelStatus;
		Button buttonConnect;
		Button buttonStop;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);

			textName = FindViewById<EditText>(Resource.Id.textName);
			textIP = FindViewById<EditText>(Resource.Id.textIP);
			labelStatus = FindViewById<TextView>(Resource.Id.labelStatus);
			buttonConnect = FindViewById<Button>(Resource.Id.buttonConnect);
			buttonStop = FindViewById<Button>(Resource.Id.buttonStop);

			Load();
			buttonConnect.Click += ButtonConnect_Click;
			buttonStop.Click += ButtonStop_Click;
		}

		private void ButtonConnect_Click(object sender, System.EventArgs e)
		{
		}

		private void ButtonStop_Click(object sender, System.EventArgs e)
		{
		}

		public override void OnBackPressed()
		{
			var dialog = new AlertDialog.Builder(this);
			dialog.SetMessage("Quit Program?");
			dialog.SetPositiveButton("Yes", (sender, e) => Quit());
			dialog.SetNegativeButton("No", (sender, e) => { });
			dialog.Show();
		}

		void Load()
		{
			using (var pref = GetPreferences(FileCreationMode.Private)) {
				textName.Text = pref.GetString("Name", "");
				textIP.Text = pref.GetString("IP", "");
			}
		}

		void Quit()
		{
			using (var pref = GetPreferences(FileCreationMode.Private)) {
				var editor = pref.Edit();
				editor.PutString("Name", textName.Text);
				editor.PutString("IP", textIP.Text);
				editor.Commit();
			}

			base.OnBackPressed();
		}
	}
}
