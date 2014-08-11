﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace POIApp
{
    [Activity(Label = "POI Detail")]			
    public class POIDetailActivity : Activity, ILocationListener
    {
        private EditText _nameEditText;
        private EditText _descEditText;
        private EditText _addrEditText;
        private EditText _latEditText;
        private EditText _longEditText;
        private ImageView _poiImageView;
        private ImageButton _locationImageButton;

        private PointOfInterest _poi;
        private LocationManager _locationManager;
        private ProgressDialog _progressDialog;

        #region Activity overrides

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.POIDetail);

            _nameEditText = FindViewById<EditText>(Resource.Id.nameEditText);
            _descEditText = FindViewById<EditText>(Resource.Id.descEditText);
            _addrEditText = FindViewById<EditText>(Resource.Id.addrEditText);
            _latEditText = FindViewById<EditText>(Resource.Id.latEditText);
            _longEditText = FindViewById<EditText>(Resource.Id.longEditText);
            _poiImageView = FindViewById<ImageView>(Resource.Id.poiImageView);
            _locationImageButton = FindViewById<ImageButton>(Resource.Id.locationImageButton);
            _locationImageButton.Click += GetLocationClicked;

            _locationManager = GetSystemService(Context.LocationService) as LocationManager;

            if (Intent.HasExtra("poiId"))
            {
                var poiId = Intent.GetIntExtra("poiId", -1);
                _poi = POIData.Service.GetPOI(poiId);
            }
            else
            {
                _poi = new PointOfInterest();
            }

            UpdateUI();
        }

        protected override void OnPause()
        {
            base.OnPause();

            _locationManager.RemoveUpdates(this);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.POIDetailMenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            base.OnPrepareOptionsMenu(menu);

            // disable delete for new POI
            if (!_poi.Id.HasValue)
            {
                IMenuItem item = menu.FindItem(Resource.Id.actionDelete);
                item.SetEnabled(false);
            }

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.actionSave:
                    SavePOI();
                    return true;

                case Resource.Id.actionDelete:
                    DeletePOI();
                    return true;

                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        #endregion

        #region ILocationListener methods

        public void OnLocationChanged(Location location)
        {
            _progressDialog.Cancel();

            _latEditText.Text = location.Latitude.ToString();
            _longEditText.Text = location.Longitude.ToString();

            var geocoder = new Geocoder(this);
            var addresses = geocoder.GetFromLocation(location.Latitude, location.Longitude, 5);

            if (addresses.Any())
                UpdateAddressFields(addresses.First());
        }

        public void OnProviderDisabled(string provider)
        { }

        public void OnProviderEnabled(string provider)
        { }

        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        { }

        #endregion

        protected void UpdateUI()
        {
            _nameEditText.Text = _poi.Name;
            _descEditText.Text = _poi.Description;
            _addrEditText.Text = _poi.Address;
            _latEditText.Text = _poi.Latitude.ToString();
            _longEditText.Text = _poi.Longitude.ToString();
        }

        private void UpdateAddressFields(Address address)
        {
            if (String.IsNullOrWhiteSpace(_nameEditText.Text))
                _nameEditText.Text = address.FeatureName;

            if (String.IsNullOrWhiteSpace(_addrEditText.Text))
            {
                for (int i = 0; i < address.MaxAddressLineIndex; i++)
                {
                    if (!String.IsNullOrWhiteSpace(_addrEditText.Text))
                    {
                        _addrEditText.Text += System.Environment.NewLine;
                        _addrEditText.Text += address.GetAddressLine(i);
                    }
                }
            }
        }

        protected void SavePOI()
        {
            var errors = validateNewPOI();

            if (!errors)
            {
                _poi.Name = _nameEditText.Text;
                _poi.Description = _descEditText.Text;
                _poi.Address = _addrEditText.Text;
                _poi.Latitude = Double.Parse(_latEditText.Text);
                _poi.Longitude = Double.Parse(_longEditText.Text);

                POIData.Service.SavePOI(_poi);

                var toast = Toast.MakeText(this, String.Format("{0} saved.", _poi.Name), ToastLength.Short);
                toast.Show();

                Finish();
            }
        }

        private bool validateNewPOI()
        {
            var errorsFound = false;

            if (String.IsNullOrWhiteSpace(_nameEditText.Text))
            {
                _nameEditText.Error = "Name cannot be empty";
                errorsFound = true;
            }
            else
                _nameEditText.Error = null;

            var tempLatLong = 0.0d;
            if (!String.IsNullOrWhiteSpace(_latEditText.Text))
            {
                var isDouble = Double.TryParse(_latEditText.Text, out tempLatLong);
                if (!isDouble || tempLatLong > 90 || tempLatLong < -90)
                {
                    _latEditText.Error = "Latitude must be a decimal value between -90 and 90";
                    errorsFound = true;
                }
                else
                    _latEditText.Error = null;
            }
            if (!String.IsNullOrWhiteSpace(_longEditText.Text))
            {
                var isDouble = Double.TryParse(_longEditText.Text, out tempLatLong);
                if (!isDouble || tempLatLong > 180 || tempLatLong < -180)
                {
                    _longEditText.Error = "Longitude must be a decimal valude between -180 and 180";
                    errorsFound = true;
                }
                else
                    _longEditText.Error = null;
            }

            return errorsFound;
        }

        protected void DeletePOI()
        {
            var alertConfirm = new AlertDialog.Builder(this);
            alertConfirm.SetCancelable(true);
            alertConfirm.SetPositiveButton("OK", ConfirmDelete);
            alertConfirm.SetNegativeButton("Cancel", delegate {});
            alertConfirm.SetMessage(String.Format("Are you sure you want to delete {0}?", _poi.Name));

            alertConfirm.Show();
        }

        private void ConfirmDelete(object sender, EventArgs e)
        {
            POIData.Service.DeletePOI(_poi);

            var toast = Toast.MakeText(this, String.Format("{0} deleted.", _poi.Name), ToastLength.Short);
            toast.Show();

            Finish();
        }

        private void GetLocationClicked(object sender, EventArgs e)
        {
            _progressDialog = ProgressDialog.Show(this, "", "Obtaining location...");

            var criteria = new Criteria
            {
                Accuracy = Accuracy.NoRequirement,
                PowerRequirement = Power.NoRequirement
            };
            var provider = _locationManager.GetBestProvider(criteria, true);

            _locationManager.RequestSingleUpdate(provider, this, null);

            var lastKnown = _locationManager.GetLastKnownLocation(provider);
            _latEditText.Text = lastKnown.Latitude.ToString();
            _longEditText.Text = lastKnown.Longitude.ToString();
        }
    }
}

