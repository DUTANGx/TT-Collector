using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Hardware;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TT_Collector
{
    [Activity(Label = "TT collector", MainLauncher = true, Icon = "@drawable/icon")]
    public class AccListener : Activity, ISensorEventListener, IDialogInterfaceOnClickListener
    {
        //components
        private SensorManager _sensorManager;
        private TextView _sensorTextView_x;
        private TextView _sensorTextView_y;
        private TextView _sensorTextView_z;
        private Button StartButton;
        private Button StopButton;
        private static readonly object _syncLock = new object();
        //variables
        StringBuilder builder = new StringBuilder();
        DateTime starttime;
        DateTime stoptime;
        double[] max = new double[4];
        double[] min = new double[4];
        double[] avg = new double[4];
        double[] sum = { 0.0, 0.0, 0.0, 0.0 };
        double[] reading = new double[4];
        //max[0]is max for resultant, [1] for x,[2] for y, [3]for z; same as min,avg,sum£¬reading
        int count = -1;
        string[] items = { "Car", "Bus", "Train", "Walk", "Bike", "Cancel" };

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            _sensorManager = (SensorManager)GetSystemService(Context.SensorService);
            _sensorTextView_x = FindViewById<TextView>(Resource.Id.accelerometer_text_x);
            _sensorTextView_y = FindViewById<TextView>(Resource.Id.accelerometer_text_y);
            _sensorTextView_z = FindViewById<TextView>(Resource.Id.accelerometer_text_z);
            StartButton = FindViewById<Button>(Resource.Id.Start);
            StopButton = FindViewById<Button>(Resource.Id.Stop);
            StopButton.Enabled = false;
            StartButton.Click += (object sender, EventArgs e) =>
            {
                starttime = DateTime.Now;
                builder.Append("{\"Type\":\"Trainning Data\",")
                       .Append(" \"StartTime\": \"" + starttime.ToString() + "\",")
                       .Append("\"Record\":[");
                StartButton.Enabled = false;
                StopButton.Enabled = true;
                //sensor activate
                _sensorManager.RegisterListener(this, _sensorManager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Ui);
            };
            base.OnResume();
            StopButton.Click += (object sender, EventArgs e) =>
            {
                //sensor close
                _sensorManager.UnregisterListener(this);
                stoptime = DateTime.Now;
                builder.Length--;
                builder.Append("],");
                builder.Append("\"EndTime\":\"" + stoptime.ToString()+"\",");
                var callDialog = new AlertDialog.Builder(this);
                callDialog.SetTitle("choose mode of transportation");
                callDialog.SetItems(items, this);
                callDialog.Show();
                StartButton.Enabled = true;
                StopButton.Enabled = false;
            };
        }
        //ready to start
        protected override void OnStart()
        {
            base.OnStart();
        }
        //running
        protected override void OnResume()
        {
            base.OnResume();

        }
        //paused
        protected override void OnPause()
        {
            base.OnPause();
            _sensorManager.UnregisterListener(this);
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {

        }

        public void OnSensorChanged(SensorEvent e)
        {
            //synchronize
            lock (_syncLock)
            {
                int i;
                reading[1] = e.Values[0];
                reading[2] = e.Values[1];
                reading[3] = e.Values[2];
                _sensorTextView_x.Text = "x:" + reading[1].ToString();
                _sensorTextView_y.Text = "y:" + reading[2].ToString();
                _sensorTextView_z.Text = "z:" + reading[3].ToString();
                reading[0] = reading[1] * reading[1] + reading[2] * reading[2] + reading[3] * reading[3];
                if (count == -1)
                {
                    for (i = 0; i < 4; i++)
                    {
                        max[i] = reading[i];
                        min[i] = reading[i];
                    }
                    count++;
                }
                if (count == 90)
                {
                    /*_sensorTextView_x.Text = "x:" + max[1].ToString();
                    _sensorTextView_y.Text = "y:" + max[2].ToString();
                    _sensorTextView_z.Text = "z:" + max[3].ToString();*/
                    //calculate resultant acceleration
                    max[0] = Math.Sqrt(max[0]);
                    min[0] = Math.Sqrt(min[0]);
                    avg[0] = sum[0] / 90;
                    avg[0] = Math.Sqrt(avg[0]);
                    for (i = 1; i < 4; i++)
                    {
                        avg[i] = sum[i] / 90;
                    }
                    //write json
                    builder.Append("{\"max_resultant\":\"" + max[0].ToString() + "\",")
                           .Append("\"min_resultant\":\"" + min[0].ToString() + "\",")
                           .Append("\"avg_resultant\":\"" + avg[0].ToString() + "\",")
                           .Append("\"max_x\":\"" + max[1].ToString() + "\",")
                           .Append("\"min_x\":\"" + min[1].ToString() + "\",")
                           .Append("\"avg_x\":\"" + avg[1].ToString() + "\",")
                           .Append("\"max_y\":\"" + max[2].ToString() + "\",")
                           .Append("\"min_y\":\"" + min[2].ToString() + "\",")
                           .Append("\"avg_y\":\"" + avg[2].ToString() + "\",")
                           .Append("\"max_z\":\"" + max[3].ToString() + "\",")
                           .Append("\"min_z\":\"" + min[3].ToString() + "\",")
                           .Append("\"avg_z\":\"" + avg[3].ToString() + "\"},");
                    //renew max min sum
                    for (i = 0; i < 4; i++)
                    {
                        max[i] = reading[i];
                        min[i] = reading[i];
                        sum[i] = 0.0;
                    }
                    count = 0;
                }
                for (i = 0; i < 4; i++)
                {
                    if (max[i] < reading[i]) max[i] = reading[i];
                    if (min[i] > reading[i]) min[i] = reading[i];
                    sum[i] += reading[i];
                }
                count++;
            }
        }

        public void OnClick(IDialogInterface dialog, int which)
        {
            switch (which)
            {
                case 0:
                    builder.Append("\"Mode\":\"Car\"}");
                    JObject obj = JObject.Parse(builder.ToString());
                    StringBuilder x = new StringBuilder();
                    foreach (var kvp in obj)
                        x.AppendLine(kvp.Key + " = " + kvp.Value);

                    this._sensorTextView_x.Text = x.ToString();
                    break;
                case 1:
                    builder.Append("\"Mode\":\"Bus\"}");
                    System.Console.WriteLine("bus");
                    break;
                case 2:
                    builder.Append("\"Mode\":\"Train\"}");
                    System.Console.WriteLine("train");
                    break;
                case 3:
                    builder.Append("\"Mode\":\"Walk\"}");
                    System.Console.WriteLine("walk");
                    break;
                case 4:
                    builder.Append("\"Mode\":\"Bike\"}");
                    System.Console.WriteLine("bike");
                    break;
                case 5:
                    break;
            }
        }
    }
}