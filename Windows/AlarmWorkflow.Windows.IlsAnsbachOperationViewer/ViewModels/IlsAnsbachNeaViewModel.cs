﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AlarmWorkflow.Parser.IlsAnsbachParser;
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.Windows.IlsAnsbachOperationViewer.ViewModels;
using AlarmWorkflow.Windows.UI;
using AlarmWorkflow.Windows.UI.Security;
using AlarmWorkflow.Windows.UI.ViewModels;

namespace AlarmWorkflow.Windows.IlsAnsbachOperationViewer
{
    class IlsAnsbachNeaViewModel : ViewModelBase
    {
        #region Static

        private static readonly OperationInformationLibrary _operationLibrary;

        static IlsAnsbachNeaViewModel()
        {
            _operationLibrary = new OperationInformationLibrary();
        }

        #endregion

        #region Fields

        private Operation _operation;
        private UIConfigurationNea _configuration;

        private ObservableCollection<ResourceViewModel> _manuallyDeployedVehicles;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the operation.
        /// </summary>
        public Operation Operation
        {
            get { return _operation; }
            set
            {
                if (value == _operation)
                {
                    return;
                }

                _operation = value;

                // Set operation itself
                OnPropertyChanged("Operation");

                // Update all other properties
                UpdateManuallyDeployedVehicles();
                UpdateProperties();
            }
        }

        /// <summary>
        /// Gets the route plan image.
        /// </summary>
        public ImageSource RoutePlanImage
        {
            get { return GetRoutePlanImage(); }
        }

        /// <summary>
        /// Gets a list containing all vehicles that have been manually deployed by pressing their associated shortkeys.
        /// </summary>
        public ObservableCollection<ResourceViewModel> ManuallyDeployedVehicles
        {
            get { return _manuallyDeployedVehicles; }
        }

        /// <summary>
        /// Gets a list containing the requested resources (only the names, without vehicles).
        /// </summary>
        public IEnumerable<RequestedResourceViewModel> RequestedResources
        {
            get { return GetRequestedResources(); }
        }

        /// <summary>
        /// Gets the value from the "Absender" custom data field.
        /// </summary>
        public string Absender
        {
            get { return GetOperationCustomData<string>("Absender", null); }
        }

        /// <summary>
        /// Gets the value from the "Termin" custom data field.
        /// </summary>
        public string Termin
        {
            get { return GetOperationCustomData<string>("Termin", null); }
        }

        /// <summary>
        /// Gets the value from the "Einsatzort Plannummer" custom data field.
        /// </summary>
        public string EinsatzortPlannummer
        {
            get { return GetOperationCustomData<string>("Einsatzort Plannummer", null); }
        }

        /// <summary>
        /// Gets the value from the "Einsatzort Station" custom data field.
        /// </summary>
        public string EinsatzortStation
        {
            get { return GetOperationCustomData<string>("Einsatzort Station", null); }
        }

        /// <summary>
        /// Gets the value from the "Zielort Hausnummer" custom data field.
        /// </summary>
        public string ZielortHausnummer
        {
            get { return GetOperationCustomData<string>("Zielort Hausnummer", null); }
        }

        /// <summary>
        /// Gets the value from the "Zielort Straße" custom data field.
        /// </summary>
        public string ZielortStrasse
        {
            get { return GetOperationCustomData<string>("Zielort Straße", null); }
        }

        /// <summary>
        /// Gets the value from the "Zielort PLZ" custom data field.
        /// </summary>
        public string ZielortPLZ
        {
            get { return GetOperationCustomData<string>("Zielort PLZ", null); }
        }

        /// <summary>
        /// Gets the value from the "Zielort Ort" custom data field.
        /// </summary>
        public string ZielortOrt
        {
            get { return GetOperationCustomData<string>("Zielort Ort", null); }
        }

        /// <summary>
        /// Gets the value from the "Zielort Objekt" custom data field.
        /// </summary>
        public string ZielortObjekt
        {
            get { return GetOperationCustomData<string>("Zielort Objekt", null); }
        }

        /// <summary>
        /// Gets the value from the "Zielort Station" custom data field.
        /// </summary>
        public string ZielortStation
        {
            get { return GetOperationCustomData<string>("Zielort Station", null); }
        }

        /// <summary>
        /// Gets the value from the "Stichwort B" custom data field.
        /// </summary>
        public string StichwortB
        {
            get { return GetOperationCustomData<string>("Stichwort B", null); }
        }

        /// <summary>
        /// Gets the value from the "Stichwort R" custom data field.
        /// </summary>
        public string StichwortR
        {
            get { return GetOperationCustomData<string>("Stichwort R", null); }
        }

        /// <summary>
        /// Gets the value from the "Stichwort S" custom data field.
        /// </summary>
        public string StichwortS
        {
            get { return GetOperationCustomData<string>("Stichwort S", null); }
        }

        /// <summary>
        /// Gets the value from the "Stichwort T" custom data field.
        /// </summary>
        public string StichwortT
        {
            get { return GetOperationCustomData<string>("Stichwort T", null); }
        }

        /// <summary>
        /// Gets the value from the "Priorität" custom data field.
        /// </summary>
        public string Priorität
        {
            get { return GetOperationCustomData<string>("Priorität", null); }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IlsAnsbachNeaViewModel"/> class.
        /// </summary>
        /// <param name="configuration"></param>
        public IlsAnsbachNeaViewModel(UIConfigurationNea configuration)
        {
            _configuration = configuration;

            _manuallyDeployedVehicles = new ObservableCollection<ResourceViewModel>();
        }

        #endregion

        #region Methods

        private T GetOperationCustomData<T>(string key, T defaultValue)
        {
            if (_operation != null && _operation.CustomData != null && _operation.CustomData.ContainsKey(key))
            {
                return (T)_operation.CustomData[key];
            }

            return defaultValue;
        }

        private ImageSource GetRoutePlanImage()
        {
            if (_operation == null)
            {
                return null;
            }
            if (_operation.RouteImage == null)
            {
                // Return dummy image
                return Helper.GetNoRouteImage();
            }

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(_operation.RouteImage);
            image.EndInit();
            return image;
        }

        private void UpdateManuallyDeployedVehicles()
        {
            // Create binding source for manually deployed vehicles and add sort description so they sort automatically
            _manuallyDeployedVehicles = new ObservableCollection<ResourceViewModel>();

            if (_operation == null)
            {
                return;
            }

            ICollectionView mdvdv = CollectionViewSource.GetDefaultView(ManuallyDeployedVehicles);
            mdvdv.SortDescriptions.Add(new SortDescription("VehicleName", ListSortDirection.Ascending));

            OperationInformation entry = _operationLibrary.GetInfoEntry(_operation.OperationNumber);
            if (entry != null)
            {
                foreach (string vehicleName in entry.ManuallyDeployedVehicleNames)
                {
                    AddManuallyDeployedVehicle(vehicleName);
                }
            }
        }

        private void UpdateProperties()
        {
            foreach (var pi in this.GetType().GetProperties().Where(p => p.Name != "Operation"))
            {
                OnPropertyChanged(pi.Name);
            }
        }

        private void AddManuallyDeployedVehicle(string resourceName)
        {
            var vehicle = _configuration.Vehicles.FirstOrDefault(v => v.Name == resourceName);
            if (vehicle == null)
            {
                return;
            }

            // Otherwise set a new resource as deployed
            ResourceViewModel rvm = new ResourceViewModel();
            rvm.VehicleName = vehicle.Name;
            rvm.SetImage(vehicle.Image);

            _manuallyDeployedVehicles.Add(rvm);
        }

        /// <summary>
        /// Adds a new manually deployed vehicle to the list.
        /// </summary>
        internal void ToggleManuallyDeployedVehicles(string resourceName)
        {
            List<Resource> dataSource = GetOperationCustomData<List<Resource>>("Einsatzmittel", null);
            if (dataSource != null)
            {
                // Get resource by its name
                var vehicle = _configuration.Vehicles.FirstOrDefault(v => v.Name == resourceName);
                if (vehicle != null)
                {
                    ResourceViewModel rvm = ManuallyDeployedVehicles.FirstOrDefault(r => r.VehicleName == vehicle.Name);
                    if (rvm != null)
                    {
                        // If this resource is already manually deployed, "undeploy" it.

                        // Require confirmation of this action
                        if (!ServiceProvider.Instance.GetService<ICredentialConfirmationDialogService>().Invoke("Eingesetztes Fahrzeug entfernen", AuthorizationMode.SimpleConfirmation))
                        {
                            return;
                        }

                        _manuallyDeployedVehicles.Remove(rvm);
                        _operationLibrary.RemoveFromInfoEntry(_operation.OperationNumber, vehicle.Name);
                    }
                    else
                    {
                        // Otherwise set a new resource as deployed
                        rvm = new ResourceViewModel();
                        rvm.VehicleName = vehicle.Name;
                        rvm.SetImage(vehicle.Image);

                        _manuallyDeployedVehicles.Add(rvm);
                        _operationLibrary.AddToInfoEntry(_operation.OperationNumber, vehicle.Name);
                    }
                }
            }
        }

        private IEnumerable<RequestedResourceViewModel> GetRequestedResources()
        {
            List<RequestedResourceViewModel> resources = new List<RequestedResourceViewModel>();

            List<Resource> dataSource = GetOperationCustomData<List<Resource>>("Einsatzmittel", null);
            if (dataSource != null)
            {
                foreach (Resource resource in dataSource)
                {
                    // Check if the filter matches
                    var vehicle = _configuration.ResourceMatches(resource.Einsatzmittel);
                    if (vehicle == null)
                    {
                        continue;
                    }

                    RequestedResourceViewModel rvm = new RequestedResourceViewModel();
                    if (!string.IsNullOrWhiteSpace(resource.GeforderteAusstattung))
                    {
                        rvm.ResourceName = resource.GeforderteAusstattung;
                    }
                    else
                    {
                        rvm.ResourceName = vehicle.Name;
                    }

                    resources.Add(rvm);
                }
            }

            return resources.OrderBy(r => r.ResourceName);
        }

        #endregion

        #region Nested types

        /// <summary>
        /// Defiens the VM for a requested resource.
        /// </summary>
        public class RequestedResourceViewModel : ViewModelBase
        {
            /// <summary>
            /// Gets/sets the name of the requested resource.
            /// </summary>
            public string ResourceName { get; set; }
        }

        /// <summary>
        /// Holds some additional information per operation.
        /// </summary>
        sealed class OperationInformationLibrary
        {
            #region Constants

            private static readonly string FilePath = Path.Combine(Utilities.GetWorkingDirectory(), "Config", "OperationInformationLibrary.csv");

            #endregion

            #region Fields

            private Dictionary<string, OperationInformation> _information;

            #endregion

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="OperationInformationLibrary"/> class.
            /// </summary>
            public OperationInformationLibrary()
            {
                _information = new Dictionary<string, OperationInformation>();
                Load();
            }

            #endregion

            #region Methods

            private OperationInformation GetInfoEntry(string operationNumber, bool addIfMissing)
            {
                if (!_information.ContainsKey(operationNumber))
                {
                    if (!addIfMissing)
                    {
                        return null;
                    }

                    _information.Add(operationNumber, new OperationInformation() { OperationNumber = operationNumber });
                }

                return _information[operationNumber];
            }

            internal OperationInformation GetInfoEntry(string operationNumber)
            {
                return GetInfoEntry(operationNumber, false);
            }

            internal void AddToInfoEntry(string operationNumber, string vehicleName)
            {
                OperationInformation oi = GetInfoEntry(operationNumber, true);
                oi.AddVehicleName(vehicleName);

                Save();
            }

            internal void RemoveFromInfoEntry(string operationNumber, string vehicleName)
            {
                OperationInformation oi = GetInfoEntry(operationNumber, true);
                oi.RemoveVehicleName(vehicleName);

                Save();
            }

            private void Save()
            {
                // TODO: Primitive - currently the whole file is saved over... not the best...
                string[] lines = new string[_information.Count];
                int i = 0;
                foreach (OperationInformation entry in _information.Values)
                {
                    lines[i] = string.Format("{0};{1}", entry.OperationNumber, string.Join(";", entry.ManuallyDeployedVehicleNames));
                    i++;
                }

                File.WriteAllLines(FilePath, lines);
            }

            private void Load()
            {
                if (!File.Exists(FilePath))
                {
                    return;
                }

                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string[] tokens = line.Split(new string[] { ";" }, System.StringSplitOptions.RemoveEmptyEntries);

                    OperationInformation entry = new OperationInformation();
                    entry.OperationNumber = tokens[0];
                    entry.ManuallyDeployedVehicleNames.AddRange(tokens.Skip(1));

                    _information[entry.OperationNumber] = entry;
                }
            }

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        sealed class OperationInformation
        {
            #region Properties

            /// <summary>
            /// Gets/sets the operation number of the associated operation.
            /// </summary>
            public string OperationNumber { get; set; }
            /// <summary>
            /// Gets/sets the names of the deployed vehicles.
            /// </summary>
            public List<string> ManuallyDeployedVehicleNames { get; set; }

            #endregion

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="OperationInformation"/> class.
            /// </summary>
            public OperationInformation()
            {
                ManuallyDeployedVehicleNames = new List<string>();
            }

            #endregion

            #region Methods

            internal void AddVehicleName(string name)
            {
                if (!ManuallyDeployedVehicleNames.Contains(name))
                {
                    ManuallyDeployedVehicleNames.Add(name);
                }
            }

            internal void RemoveVehicleName(string name)
            {
                if (ManuallyDeployedVehicleNames.Contains(name))
                {
                    ManuallyDeployedVehicleNames.Remove(name);
                }
            }

            #endregion
        }

        #endregion
    }
}