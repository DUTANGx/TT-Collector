using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using Android.Hardware;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

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

        //function
        /*private void saveFile() {
            var folder = Android.OS.Environment.ExternalStorageDirectory + Java.IO.File.Separator + "TTdata";
            var filename = folder +
                              Java.IO.File.Separator +
                              "aa" + ".json";

            try
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                using (var fs = new FileStream(filename, FileMode.OpenOrCreate))
                {
                    byte[] byteFile = Encoding.UTF8.GetBytes(builder.ToString());
                    //参数：要写入到文件的数据数组，从数组的第几个开始写，一共写多少个字节
                    fs.Write(byteFile, 0, byteFile.Length);
                }
            }
            catch
            {
                RunOnUiThread(() =>
                {
                    var builders = new AlertDialog.Builder(this);
                    builders.SetMessage("Saving image went wrong");
                    builders.SetTitle("Unable to save image");
                    builders.Show();
                });
            }
        }*/



        //variables
        string sas = "https://ucltt.blob.core.windows.net/collector/?sv=2015-04-05&sr=c&sig=E3KK%2BaWJVw8vemkDM8%2BsV9n7K5SLdgstXX1RuSTvBsc%3D&st=2015-12-14T11%3A30%3A06Z&se=2016-06-14T10%3A30%3A06Z&sp=rwdl";
        StringBuilder builder = new StringBuilder();
        DateTime starttime;
        DateTime stoptime;
        double[] max = new double[4];
        double[] min = new double[4];
        double[] avg = new double[4];
        double[] sum = { 0.0, 0.0, 0.0, 0.0 };
        double[] reading = new double[4];
        //max[0]is max for resultant, [1] for x,[2] for y, [3]for z; same as min,avg,sum，reading
        int count = -1;
        string[] items = { "Car", "Bus", "Train", "Metro", "Walk", "Bike", "Cancel" };

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
            StopButton.Click += (object sender, EventArgs e) =>
            {
                //sensor close
                _sensorManager.UnregisterListener(this);
                StartButton.Enabled = true;
                StopButton.Enabled = false;
                stoptime = DateTime.Now;
                builder.Length--;
                builder.Append("],");
                builder.Append("\"EndTime\":\"" + stoptime.ToString()+"\",");
                var callDialog = new AlertDialog.Builder(this);
                callDialog.SetTitle("choose mode of transportation");
                callDialog.SetItems(items, this);
                callDialog.Show();
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
            //_sensorManager.UnregisterListener(this);
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
                    builder.Append("{\"max_resultant\":" + max[0].ToString() + ",")
                           .Append("\"min_resultant\":" + min[0].ToString() + ",")
                           .Append("\"avg_resultant\":" + avg[0].ToString() + ",")
                           .Append("\"max_x\":" + max[1].ToString() + ",")
                           .Append("\"min_x\":" + min[1].ToString() + ",")
                           .Append("\"avg_x\":" + avg[1].ToString() + ",")
                           .Append("\"max_y\":" + max[2].ToString() + ",")
                           .Append("\"min_y\":" + min[2].ToString() + ",")
                           .Append("\"avg_y\":" + avg[2].ToString() + ",")
                           .Append("\"max_z\":" + max[3].ToString() + ",")
                           .Append("\"min_z\":" + min[3].ToString() + ",")
                           .Append("\"avg_z\":" + avg[3].ToString() + "},");
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
        public async void OnClick(IDialogInterface dialog, int which)
        {
            switch (which)
            {
                case 0:
                    builder.Append("\"Mode\":\"Car\"}");              
                    await UseContainerSAS(sas,"Car",builder.ToString());
                    builder.Length = 0;
                    break;
                case 1:
                    builder.Append("\"Mode\":\"Bus\"}");
                    await UseContainerSAS(sas, "Bus", builder.ToString());
                    builder.Length = 0;
                    break;
                case 2:
                    builder.Append("\"Mode\":\"Train\"}");
                    await UseContainerSAS(sas, "Train", builder.ToString());
                    builder.Length = 0;
                    break;
                case 3:
                    builder.Append("\"Mode\":\"Metro\"}");
                    await UseContainerSAS(sas, "Metro", builder.ToString());
                    builder.Length = 0;
                    break;
                case 4:
                    builder.Append("\"Mode\":\"Walk\"}");
                    await UseContainerSAS(sas, "Walk", builder.ToString());
                    builder.Length = 0;
                    break;
                case 5:
                    builder.Append("\"Mode\":\"Bike\"}");
                    await UseContainerSAS(sas, "Bike", builder.ToString());
                    builder.Length = 0;
                    break;
                case 6:
                    builder.Length = 0;
                    break;
            }
        }
        static async Task UseContainerSAS(string sas, string mode, string json)
        {
            //Try performing container operations with the SAS provided.

            //break a reference to the container using the SAS URI.
            CloudBlobContainer container = new CloudBlobContainer(new Uri(sas));
            string date = DateTime.Now.ToString();
            try
            {
                //Write operation: write a new blob to the container.
                CloudBlockBlob blob = container.GetBlockBlobReference(mode + date + ".json");

                string blobContent = json;
                MemoryStream msWrite = new
                MemoryStream(Encoding.UTF8.GetBytes(blobContent));
                msWrite.Position = 0;
                using (msWrite)
                {
                    await blob.UploadFromStreamAsync(msWrite);
                }
                Console.WriteLine("Write operation succeeded for SAS " + sas);
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Write operation failed for SAS " + sas);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
        }
    }
}