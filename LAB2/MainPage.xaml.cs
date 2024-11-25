using LAB2.Additional_Buttons_Functionality;
using LAB2.Attributes;
using LAB2.Strategies;
using LAB2.Google_Drive_Services;
using System.Text;
using System.Xml;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System.Diagnostics;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using LAB2.Protocol;

namespace LAB2
{
    public partial class MainPage : ContentPage
    {
        private StrategyParser _strategyParser;
        private string _selectedFilePath;
        private Stream _stream;
        private Filter _filter = new Filter();
        private IEnumerable<Student.Student> _students;
        private DriveService? _service;
        private readonly string[] _scopes = { DriveService.Scope.Drive };
        private Logger _logger = Logger.Instance;


        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnLoadFileClicked(object sender, EventArgs e)
        {
            try
            {
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".xml" } },
                    { DevicePlatform.MacCatalyst, new[] { ".xml" } },
                    { DevicePlatform.iOS, new[] { "public.xml" } },
                    { DevicePlatform.Android, new[] { "application/xml" } }
                });

                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select an XML File",
                    FileTypes = customFileType
                });

                if (result is not null)
                {
                    _selectedFilePath = result.FullPath;

                    _logger.Log(LogLevel.SELECTION, $"XML File at {_selectedFilePath} selected");

                    var options = OnLoadFileClickedClass.LoadOptionsFromXML(_selectedFilePath);

                    GeneratePickers(options);

                    await DisplayAlert("XML Loaded", "XML file loaded successfully", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnLoadXMLfromGoogleDriveClicked(object sender, EventArgs e)
        {
            try
            {
                if (_service is null)
                    _service = await Authentication.GetDriveServiceAsync(_scopes);

                var forLoading = new LoadFiles();
                await forLoading.LoadFilesAsync(_service, _scopes, FileTypes.XML, this);
                var options = forLoading.options;
                _stream = forLoading.stream;

                _logger.Log(LogLevel.SELECTION, $"File from Google Drive at {_stream} selected");

                GeneratePickers(options);

                await DisplayAlert("XML Loaded", "XML file loaded successfully", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnAnalyzeClicked(object sender, EventArgs e)
        { 
            string? parser = ParserPicker.SelectedItem.ToString();

            switch (parser)
            {
                case "SAX API":
                    _strategyParser = new StrategyParser(new StrategySAXAPI());
                    _logger.Log(LogLevel.SELECTION, "SAX API selected");
                    break;
                case "DOM API":
                    _strategyParser = new StrategyParser(new StrategyDOMAPI());
                    _logger.Log(LogLevel.SELECTION, "DOM API selected");
                    break;
                case "LINQ to XML":
                    _strategyParser = new StrategyParser(new StrategyLINQtoXML());
                    _logger.Log(LogLevel.SELECTION, "LINQ to XML selected");
                    break;
                default:
                    await DisplayAlert("Error", "Please select a parser", "OK");
                    return;
            }

            try 
            {
                if (_selectedFilePath is null && _stream is not null)
                {
                    _students = _strategyParser.Parse(_stream, _filter);
                }
                else if (_selectedFilePath is not null && _stream is null)
                {
                    _students = _strategyParser.Parse(_selectedFilePath, _filter);
                }
                else
                {
                    throw new Exception("Unkown Behavior");
                }

                _logger.Log(LogLevel.TRANSFORMATION, "Result parsed");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }            

            ResultsLabel.Text = OnAnalyzeClickedClass.FormatResults(_students);
        }

        private async void OnTransformClicked(object sender, EventArgs e)
        {
            try
            {
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".xls", ".xlsx" } },
                    { DevicePlatform.MacCatalyst, new[] { "com.microsoft.excel.xls", "org.openxmlformats.spreadsheetml.sheet" } },
                    { DevicePlatform.iOS, new[] { "com.microsoft.excel.xls", "org.openxmlformats.spreadsheetml.sheet" } },
                    { DevicePlatform.Android, new[] { "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } }
                });

                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select an Excel",
                    FileTypes = customFileType
                });

                if (result is not null)
                {
                    string selectedFilePath = result.FullPath;

                    _logger.Log(LogLevel.SELECTION, $"XLSX File at {selectedFilePath} selected");

                    OnTransformClickedClass.WriteStudentsToExcel(_students, selectedFilePath);
                    OnTransformClickedClass.ConvertToHTML(selectedFilePath);
                    _students = Enumerable.Empty<Student.Student>();

                    _logger.Log(LogLevel.SAVING, "HTML File formed");

                    await DisplayAlert("HTML formed", "HTML file formed successfully", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnTransformWithGoogleDriveClicked(object sender, EventArgs e)
        {
            if (_service is null)
                _service = await Authentication.GetDriveServiceAsync(_scopes);

            try
            {
                var forLoading = new LoadFiles();
                await forLoading.LoadFilesAsync(_service, _scopes, FileTypes.XLSX, this);
                _stream = forLoading.stream;

                _logger.Log(LogLevel.SELECTION, $"XLSX File from Google Drive at {_stream} selected");

                if (_stream is null)
                    throw new Exception("Failed to load student file.");

                _stream.Position = 0;

                UploadHTMLtoGoogleDrive.WriteStudentsToExcel(_students, _stream);

                _stream.Position = 0;

                string htmlContent = UploadHTMLtoGoogleDrive.ConvertToHTML(_stream);

                await UploadHTMLtoGoogleDrive.UploadFileToGoogleDrive(htmlContent, "StudentsTable.html", _service, this);

                _logger.Log(LogLevel.SAVING, "HTML File saved to Google Drive");

                _students = Enumerable.Empty<Student.Student>();

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            ResultsLabel.Text = "Results will appear here...";
            ParserPicker.SelectedIndex = -1;
            _filter = new Filter();

            for (int i = 0; i < DynamicPickersContainer.Children.Count; i++)
            {
                if (DynamicPickersContainer.Children[i] is Picker picker)
                {
                    picker.SelectedIndex = -1;
                }
            }

            _logger.Log(LogLevel.TRANSFORMATION, "Clear Button clicked");
        }

        private async void OnExitClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Confirmation", "Do you really want to exit?", "Yes", "No");
            if (answer)
            {
                _logger.Log(LogLevel.TRANSFORMATION, "Exiting Program");
                System.Environment.Exit(0);
            }
        }

        private async void OnSignOutCLicked(object sender, EventArgs e)
        {
            if (_service is null)
            {
                _logger.Log(LogLevel.TRANSFORMATION, "User is not signed in");
                await DisplayAlert("Error", "You are not signed in", "OK");
            }
            else
            {
                _service = null;
                _logger.Log(LogLevel.TRANSFORMATION, "User has been signed out");
                await DisplayAlert("Signed out", "You have been signed out", "OK");
            }
            
        }

        private void CreatePicker(string title, List<string?> items)
        {

            var picker = new Picker
            {
                Title = $"Select {title}",
                FontSize = 14,
                ItemsSource = items
            };

            picker.SelectedIndexChanged += (sender, e) =>
            {
                var selectedPicker = (Picker)sender;

                if (selectedPicker.SelectedIndex < 0)
                    return;

                var selectedValue = selectedPicker.SelectedItem.ToString();

                switch (title)
                {
                    case "First Name":
                        _filter.FirstName = selectedValue;
                        break;
                    case "Last Name":
                        _filter.LastName = selectedValue;
                        break;
                    case "Faculty":
                        _filter.Faculty = selectedValue;
                        break;
                    case "Cathedra":
                        _filter.Cathedra = selectedValue;
                        break;
                    case "Course":
                        _filter.Course = selectedValue;
                        break;
                    case "Address":
                        _filter.Address = selectedValue;
                        break;
                    case "Start Date":
                        _filter.StartDate = selectedValue;
                        break;
                    case "End Date":
                        _filter.EndDate = selectedValue;
                        break;
                    case "Room Number":
                        _filter.RoomNumber = selectedValue;
                        break;
                }
            };

            DynamicPickersContainer.Children.Add(picker);
        }

        private void GeneratePickers(AttributesOptions? options)
        {
            DynamicPickersContainer.Children.Clear();

            CreatePicker("First Name", options.FirstNames);
            CreatePicker("Last Name", options.LastNames);
            CreatePicker("Faculty", options.Faculties);
            CreatePicker("Cathedra", options.Cathedras);
            CreatePicker("Course", options.Courses);
            CreatePicker("Address", options.Addresses);
            CreatePicker("Start Date", options.StartDates);
            CreatePicker("End Date", options.EndDates);
            CreatePicker("Room Number", options.RoomNumbers);
        }
    }

}
