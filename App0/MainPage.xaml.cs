using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App0
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>http://datapoint.metoffice.gov.uk/public/data/resource?key=a3829754-3c64-4148-b5bd-c1f221c18c3e
    /// //{"elevation":"69.0","id":"310114","latitude":"52.2401","longitude":"-0.9011","name":"Northampton","region":"em","unitaryAuthArea":"Northamptonshire"},
    /// Sample API:http://datapoint.metoffice.gov.uk/public/data/val/wxfcs/all/xml/310114?res=3hourly&key=a3829754-3c64-4148-b5bd-c1f221c18c3e
    public sealed partial class MainPage : Page
    {
        private MqttClient client;
        byte[] message;
        string Location;
        string busLongitude, busLatitude;
        private string timeNow;
        BasicGeoposition geoPosition = new BasicGeoposition();
        MapIcon busIcon = new MapIcon
        {
            //Location = myLocationPoint,
            //Title = newBusLocation.busID,
            NormalizedAnchorPoint = new Point(0.5, 1.0),
            ZIndex = 10
        };

        public MainPage()
        {
            this.InitializeComponent();
            DateTime localDate = DateTime.Now;
            textTime.Text = localDate.ToString();
            timeNow = localDate.ToString();
            this.client = new MqttClient("broker.mqttdashboard.com");//QOS_LEVEL_AT_MOST_ONCE
            try
            {
                this.client.Connect(Guid.NewGuid().ToString());
                Debug.WriteLine("broker.mqttdashboard.com");
            }
            catch (Exception)
            {
                try
                {
                    this.client = new MqttClient("iot.eclipse.org");
                    this.client.Connect(Guid.NewGuid().ToString());
                    Debug.WriteLine("iot.eclipse.org");
                }
                catch (Exception)
                {

                   Debug.WriteLine("No Broker Connection");
                }
                byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };
                this.client.Publish("/MQTTpublisher", Encoding.UTF8.GetBytes("connected"));
            }
            this.client.Subscribe(new string[] { "/busLocation" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            this.client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
        }

        private async void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            BusLocation newBusLocation = new BusLocation();
            

            if (e.Topic.ToString() == "/busLocation")
            {
                Debug.WriteLine("/busLocation");
                message = e.Message;
                StringBuilder parsingMsg = new StringBuilder();
                foreach (var value in message)
                {
                    parsingMsg.Append(Char.ConvertFromUtf32(value));
                }
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    string jsonMSG = parsingMsg.ToString();
                    try
                    {
                        Newtonsoft.Json.JsonConvert.PopulateObject(jsonMSG, newBusLocation);
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                    
                    geoPosition.Latitude = Convert.ToDouble(newBusLocation.latitude);
                    geoPosition.Longitude = Convert.ToDouble(newBusLocation.longitude);

                    Debug.WriteLine("Location  Received: Lat:" + newBusLocation.latitude +" long : " +newBusLocation.longitude);
                    loadPointView(geoPosition.Latitude, geoPosition.Longitude);
                   
                   
                    
                    
                    

                });
            }
            //else if (e.Topic.ToString() == "/busLON")
            //{
            //    Debug.WriteLine("bUSlong");
            //    message = e.Message;
            //    StringBuilder parsingMsg = new StringBuilder();
            //    foreach (var value in message)
            //    {
            //        parsingMsg.Append(Char.ConvertFromUtf32(value));
            //    }
            //    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //    {
            //        busLongitude = parsingMsg.ToString();
            //        Debug.WriteLine("Location received Received" + busLongitude);
            //    });
            //}
            else
            {
                //message = e.Message;
                //StringBuilder parsingMsg = new StringBuilder();
                //foreach (var value in message)
                //{
                //    parsingMsg.Append(Char.ConvertFromUtf32(value));
                //}
                //await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                //{
                //    Location = parsingMsg.ToString();
                //    Debug.WriteLine("Location received Received" + Location);
                //});
            }
            
        }

        public void myMap_Loaded(object sender, RoutedEventArgs e)
        {
            myMap.Style = Windows.UI.Xaml.Controls.Maps.MapStyle.Aerial3DWithRoads;
            //token
            myMap.MapServiceToken = "7pMH5pZF11w5Le16l05Z~qHzlmpJOSHtDcBFeFsDFPw~AtwucQgJteVmOGzegiZRQ8kELpCJvoot8Ssuq96znzAN5s3_Jq1Ne53hnLd2cuyR";

            
            geoPosition.Latitude = 52.250465;
            geoPosition.Longitude = -0.889620;

            Geopoint myLocationPoint = new Geopoint(geoPosition);
            myMap.Center = myLocationPoint;
            myMap.ZoomLevel = 15;

            loadPointView(52.250465, -0.889620);

            

            startLocation.Click += (o, i) =>
            {
                Debug.WriteLine("Some");
                myMap.Center = myLocationPoint;
                myMap.ZoomLevel = 15;
            };
        }

        public BasicGeoposition loadPointView(double Lat,double Lon)
        {

            myMap.MapElements.Remove(busIcon);
            BasicGeoposition newBGeo = new BasicGeoposition();
            newBGeo.Latitude = Lat;
            newBGeo.Longitude = Lon;

            Geopoint myLocationPoint = new Geopoint(newBGeo);

            busIcon.Location = myLocationPoint;
            busIcon.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/Gps_circle_small.png"));
            myMap.MapElements.Add(busIcon);
            myMap.Center = myLocationPoint;
            myMap.ZoomLevel = 15;
            
            return newBGeo;
        }

        private void sliderZoom_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            myMap.ZoomLevel = sliderZoom.Value;
        }

        public void button_Click(object sender, RoutedEventArgs e)
        {
           // myMap.Center = 
        }

        private void image_Tapped(object sender, TappedRoutedEventArgs e)
        {

            Debug.WriteLine("image triggered");
            string topicStringA = "RequestDisableUser";
            this.client.Publish(topicStringA, Encoding.UTF8.GetBytes("active"));
        }
    }
}
