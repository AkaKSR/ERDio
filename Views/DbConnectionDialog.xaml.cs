using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using ERDio.Models;
using ERDio.Services;

namespace ERDio;

/// <summary>
/// Interaction logic for DbConnectionDialog.xaml
/// </summary>
public partial class DbConnectionDialog : Window
{
    public List<Table>? GeneratedTables { get; private set; }
    public List<Relationship>? GeneratedRelationships { get; private set; }
    public bool IsSuccess { get; private set; }

    private static readonly Dictionary<string, int> DefaultPorts = new()
    {
        { "MySQL", 3306 },
        { "PostgreSQL", 5432 },
        { "Oracle", 1521 },
        { "Tibero", 8629 }
    };

    private const string ProfilesFileName = "erdio_profiles.json";
    private string ProfilesFilePath = string.Empty;
    private List<ProfileRecord> _profiles = new();

    private class ProfileRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string DbType { get; set; } = "MySQL";
        public string UserId { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
    }

    public DbConnectionDialog()
    {
        InitializeComponent();
        // prepare profiles file path
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ERDio");
            Directory.CreateDirectory(dir);
            ProfilesFilePath = Path.Combine(dir, ProfilesFileName);
        }
        catch
        {
            ProfilesFilePath = ProfilesFileName;
        }

        LoadProfiles();
    }

    private void OnDbTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        // Prevent null reference during initialization
        if (PortTextBox == null || DatabaseLabel == null || SchemaLabel == null || SchemaTextBox == null) return;

        if (DbTypeCombo.SelectedItem is ComboBoxItem selectedItem)
        {
            string dbType = selectedItem.Tag?.ToString() ?? "MySQL";
            
            // Update default port
            if (DefaultPorts.TryGetValue(dbType, out int port))
            {
                PortTextBox.Text = port.ToString();
            }
            
            // Update Database/Schema label and visibility based on DB type
            if (dbType == "Oracle" || dbType == "Tibero")
            {
                DatabaseLabel.Text = "Schema:";
                SchemaLabel.Visibility = Visibility.Collapsed;
                SchemaTextBox.Visibility = Visibility.Collapsed;
            }
            else if (dbType == "PostgreSQL")
            {
                DatabaseLabel.Text = "Database:";
                SchemaLabel.Visibility = Visibility.Visible;
                SchemaTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                DatabaseLabel.Text = "Database:";
                SchemaLabel.Visibility = Visibility.Collapsed;
                SchemaTextBox.Visibility = Visibility.Collapsed;
            }
        }
    }
        private void OnSaveProfile(object sender, RoutedEventArgs e)
        {
            try
            {
                string profileName = ProfileName.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(profileName))
                {
                    MessageBox.Show("Please enter a profile name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ProfileName.Focus();
                    return;
                }

                int port = 0;
                if (!int.TryParse(PortTextBox.Text, out port))
                {
                    port = DefaultPorts.TryGetValue((DbTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MySQL", out var dport) ? dport : 0;
                }

                var existing = _profiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
                string selectedDbType = (DbTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MySQL";
                if (existing != null)
                {
                    var res = MessageBox.Show($"Profile '{profileName}' already exists. Overwrite?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res != MessageBoxResult.Yes) return;

                    existing.Host = HostTextBox.Text.Trim();
                    existing.Port = port;
                    existing.DbType = selectedDbType;
                    existing.UserId = IdTextBox.Text.Trim();
                    existing.EncryptedPassword = EncryptString(PasswordBox.Password);
                    existing.Database = DatabaseTextBox.Text.Trim();
                    existing.Schema = SchemaTextBox.Text.Trim();
                }
                else
                {
                    var rec = new ProfileRecord
                    {
                        Name = profileName,
                        Host = HostTextBox.Text.Trim(),
                        Port = port,
                        DbType = selectedDbType,
                        UserId = IdTextBox.Text.Trim(),
                        EncryptedPassword = EncryptString(PasswordBox.Password),
                        Database = DatabaseTextBox.Text.Trim(),
                        Schema = SchemaTextBox.Text.Trim()
                    };
                    _profiles.Add(rec);
                }

                SaveProfilesToFile();
                UpdateProfileListUI();
                DescriptionListBox.SelectedItem = profileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save profile:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRemoveProfile(object sender, RoutedEventArgs e)
        {
            if (DescriptionListBox.SelectedItem is string name)
            {
                var res = MessageBox.Show($"Remove profile '{name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;

                var rec = _profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (rec != null)
                {
                    _profiles.Remove(rec);
                    try
                    {
                        SaveProfilesToFile();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to remove profile:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    UpdateProfileListUI();
                }
            }
            else
            {
                MessageBox.Show("Please select a profile to remove.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnProfileSelected(object sender, SelectionChangedEventArgs e)
        {
            if (DescriptionListBox.SelectedItem is string name)
            {
                var rec = _profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (rec != null)
                {
                    ProfileName.Text = rec.Name;

                    // Set DB type selection first so UI (labels/default port) updates accordingly,
                    // then overwrite port with saved value.
                    var item = DbTypeCombo.Items.OfType<ComboBoxItem>().FirstOrDefault(ci => (ci.Tag?.ToString() ?? string.Empty).Equals(rec.DbType, StringComparison.OrdinalIgnoreCase));
                    if (item != null)
                    {
                        DbTypeCombo.SelectedItem = item;
                    }

                    HostTextBox.Text = rec.Host;
                    PortTextBox.Text = rec.Port.ToString();
                    IdTextBox.Text = rec.UserId;
                    try
                    {
                        PasswordBox.Password = DecryptString(rec.EncryptedPassword);
                    }
                    catch
                    {
                        PasswordBox.Password = string.Empty;
                    }
                    DatabaseTextBox.Text = rec.Database;
                    SchemaTextBox.Text = rec.Schema;
                }
            }
        }

        private void SaveProfilesToFile()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_profiles, options);
            File.WriteAllText(ProfilesFilePath, json, Encoding.UTF8);
        }

        private void LoadProfiles()
        {
            try
            {
                if (File.Exists(ProfilesFilePath))
                {
                    var json = File.ReadAllText(ProfilesFilePath, Encoding.UTF8);
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _profiles = JsonSerializer.Deserialize<List<ProfileRecord>>(json, opts) ?? new List<ProfileRecord>();
                }
                else
                {
                    _profiles = new List<ProfileRecord>();
                }

                UpdateProfileListUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load profiles:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _profiles = new List<ProfileRecord>();
            }
        }

        private void UpdateProfileListUI()
        {
            DescriptionListBox.Items.Clear();
            foreach (var p in _profiles)
            {
                DescriptionListBox.Items.Add(p.Name);
            }
        }

        private static string EncryptString(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return string.Empty;
            var bytes = Encoding.UTF8.GetBytes(plain);
            var enc = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(enc);
        }

        private static string DecryptString(string encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64)) return string.Empty;
            var bytes = Convert.FromBase64String(encryptedBase64);
            var dec = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(dec);
        }

    private void OnPortPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow numeric input
        e.Handled = !IsNumeric(e.Text);
    }

    private static bool IsNumeric(string text)
    {
        return Regex.IsMatch(text, @"^[0-9]+$");
    }

    private async void OnExecute(object sender, RoutedEventArgs e)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(HostTextBox.Text))
        {
            MessageBox.Show("Please enter the Host.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            HostTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(PortTextBox.Text) || !int.TryParse(PortTextBox.Text, out int port))
        {
            MessageBox.Show("Please enter a valid Port number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            PortTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(IdTextBox.Text))
        {
            MessageBox.Show("Please enter the ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            IdTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(DatabaseTextBox.Text))
        {
            string fieldName = DatabaseLabel.Text.TrimEnd(':');
            MessageBox.Show($"Please enter the {fieldName}.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            DatabaseTextBox.Focus();
            return;
        }

        string dbType = (DbTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MySQL";
        string host = HostTextBox.Text.Trim();
        string userId = IdTextBox.Text.Trim();
        string password = PasswordBox.Password;
        string database = DatabaseTextBox.Text.Trim();
        string schema = SchemaTextBox.Text.Trim();

        // Disable controls during execution
        IsEnabled = false;
        Cursor = Cursors.Wait;

        try
        {
            var dbService = new DatabaseService();
            var connectionInfo = new DatabaseConnectionInfo
            {
                DbType = dbType,
                Host = host,
                Port = port,
                UserId = userId,
                Password = password,
                Database = database,
                Schema = dbType == "PostgreSQL" ? schema : string.Empty
            };

            var result = await Task.Run(() => dbService.GenerateErdFromDatabase(connectionInfo));

            if (result.IsSuccess)
            {
                GeneratedTables = result.Tables;
                GeneratedRelationships = result.Relationships;
                IsSuccess = true;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show($"Failed to generate ERD:\n{result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Connection error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsEnabled = true;
            Cursor = Cursors.Arrow;
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

}