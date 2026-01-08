using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Security.Principal;
using WinGetUI.Models;

namespace WinGetUI.Services
{
    /// <summary>
    /// Search filter type for package searches
    /// </summary>
    public enum SearchFilterType
    {
        Name,   // Search by package name
        Id,     // Search by package ID
        Both    // Search by both name and ID
    }

    /// <summary>
    /// Service for interacting with Windows Package Manager (winget)
    /// </summary>
    public class WingetService
    {
        private const string WINGET_PATH = "winget";

        /// <summary>
        /// Gets list of installed packages
        /// </summary>
        public async Task<List<Package>> GetInstalledPackagesAsync()
        {
            try
            {
                var output = await ExecuteWingetCommandAsync("list");
                return ParsePackageList(output, PackageStatus.Installed);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting installed packages: {ex.Message}");
                return new List<Package>();
            }
        }

        /// <summary>
        /// Gets list of available packages (upgradable)
        /// </summary>
        public async Task<List<Package>> GetUpgradablePackagesAsync()
        {
            try
            {
                var output = await ExecuteWingetCommandAsync("upgrade");
                return ParseUpgradeList(output);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting upgradable packages: {ex.Message}");
                return new List<Package>();
            }
        }

        /// <summary>
        /// Searches for available packages to install
        /// </summary>
        public async Task<List<Package>> SearchPackagesAsync(string query)
        {
            try
            {
                var output = await ExecuteWingetCommandAsync($"search \"{query}\"");
                var packages = ParsePackageList(output, PackageStatus.Available);
                Debug.WriteLine($"Search for '{query}': Found {packages.Count} packages");
                foreach (var pkg in packages)
                {
                    Debug.WriteLine($"  - {pkg.Name} ({pkg.Id}) v{pkg.Version}");
                }
                return packages;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching packages: {ex.Message}");
                return new List<Package>();
            }
        }

        /// <summary>
        /// Advanced search with filtering by field and match type
        /// </summary>
        public async Task<List<Package>> SearchPackagesAdvancedAsync(string query, SearchFilterType filterType, bool exactMatch = false)
        {
            var allResults = await SearchPackagesAsync(query);
            
            List<Package> filtered;
            if (exactMatch)
            {
                filtered = FilterPackagesExact(allResults, query, filterType);
            }
            else
            {
                filtered = FilterPackagesPartial(allResults, query, filterType);
            }
            
            Debug.WriteLine($"Advanced search '{query}' ({filterType}, {(exactMatch ? "exact" : "partial")}): {filtered.Count} results after filtering");
            return filtered;
        }

        private List<Package> FilterPackagesExact(List<Package> packages, string query, SearchFilterType filterType)
        {
            query = query.ToLowerInvariant();
            
            return filterType switch
            {
                SearchFilterType.Name => packages.Where(p => p.Name.Equals(query, StringComparison.OrdinalIgnoreCase)).ToList(),
                SearchFilterType.Id => packages.Where(p => p.Id.Equals(query, StringComparison.OrdinalIgnoreCase)).ToList(),
                SearchFilterType.Both => packages.Where(p => 
                    p.Name.Equals(query, StringComparison.OrdinalIgnoreCase) || 
                    p.Id.Equals(query, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => packages
            };
        }

        private List<Package> FilterPackagesPartial(List<Package> packages, string query, SearchFilterType filterType)
        {
            query = query.ToLowerInvariant();
            
            return filterType switch
            {
                SearchFilterType.Name => packages.Where(p => p.Name.ToLowerInvariant().Contains(query)).ToList(),
                SearchFilterType.Id => packages.Where(p => p.Id.ToLowerInvariant().Contains(query)).ToList(),
                SearchFilterType.Both => packages.Where(p => 
                    p.Name.ToLowerInvariant().Contains(query) || 
                    p.Id.ToLowerInvariant().Contains(query)).ToList(),
                _ => packages
            };
        }

        /// <summary>
        /// Installs a package
        /// </summary>
        public async Task<OperationResult> InstallPackageAsync(string packageId, bool silent = false)
        {
            try
            {
                string cmd = $"install --id {packageId} --accept-package-agreements --accept-source-agreements";
                if (silent)
                {
                    cmd += " --silent --disable-interactivity";
                }
                
                var output = await ExecuteWingetCommandAsync(cmd);
                
                if (output.Contains("Successfully installed") || output.Contains("already installed"))
                {
                    return OperationResult.CreateSuccess($"Package {packageId} installed successfully");
                }

                return OperationResult.CreateError($"Installation may have failed. Output: {output}");
            }
            catch (Exception ex)
            {
                return OperationResult.CreateError($"Installation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates a package
        /// </summary>
        public async Task<OperationResult> UpdatePackageAsync(string packageId, bool silent = false)
        {
            try
            {
                string cmd = $"upgrade --id {packageId} --accept-package-agreements --accept-source-agreements";
                if (silent)
                {
                    cmd += " --silent --disable-interactivity";
                }

                var output = await ExecuteWingetCommandAsync(cmd);
                
                if (output.Contains("Successfully installed") || output.Contains("already the latest"))
                {
                    return OperationResult.CreateSuccess($"Package {packageId} updated successfully");
                }

                return OperationResult.CreateError($"Update may have failed. Output: {output}");
            }
            catch (Exception ex)
            {
                return OperationResult.CreateError($"Update failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Uninstalls a package
        /// </summary>
        public async Task<OperationResult> UninstallPackageAsync(string packageId, bool silent = false)
        {
            try
            {
                string cmd = $"uninstall --id {packageId} --accept-source-agreements";
                if (silent)
                {
                    cmd += " --silent --disable-interactivity";
                }

                var output = await ExecuteWingetCommandAsync(cmd);
                
                if (output.Contains("Successfully uninstalled"))
                {
                    return OperationResult.CreateSuccess($"Package {packageId} uninstalled successfully");
                }

                return OperationResult.CreateError($"Uninstall may have failed. Output: {output}");
            }
            catch (Exception ex)
            {
                return OperationResult.CreateError($"Uninstall failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets list of package sources
        /// </summary>
        public async Task<List<PackageSource>> GetPackageSourcesAsync()
        {
            try
            {
                var output = await ExecuteWingetCommandAsync("source list");
                return ParseSourceList(output);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting package sources: {ex.Message}");
                return new List<PackageSource>();
            }
        }

        /// <summary>
        /// Adds a new package source
        /// </summary>
        public async Task<OperationResult> AddSourceAsync(string name, string url, string type = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return OperationResult.CreateError("Source name is required");
                
                if (string.IsNullOrWhiteSpace(url))
                    return OperationResult.CreateError("Source URL/argument is required");

                string arguments = $"source add --name \"{name}\" --arg \"{url}\" --accept-source-agreements";
                if (!string.IsNullOrWhiteSpace(type))
                    arguments += $" --type \"{type}\"";

                string wingetPath = ResolveWingetPath();
                string output;
                if (IsRunningAsAdmin())
                {
                    output = await ExecuteWingetCommandAsync(arguments);
                }
                else
                {
                    output = await ExecuteWingetCommandElevatedAsync(arguments);
                }
                
                if (output.Contains("successfully") || output.Contains("added"))
                {
                    // Update sources to ensure new source is indexed for search
                    await UpdateSourceAsync(name);
                    return OperationResult.CreateSuccess($"Source '{name}' added successfully");
                }

                return OperationResult.CreateError($"Failed to add source. Output: {output}");
            }
            catch (Exception ex)
            {
                return OperationResult.CreateError($"Failed to add source: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes a package source
        /// </summary>
        public async Task<OperationResult> RemoveSourceAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return OperationResult.CreateError("Source name is required");

                string arguments = $"source remove --name \"{name}\"";
                
                string output;
                if (IsRunningAsAdmin())
                {
                    output = await ExecuteWingetCommandAsync(arguments);
                }
                else
                {
                    output = await ExecuteWingetCommandElevatedAsync(arguments);
                }

                if (output.Contains("successfully") || output.Contains("removed"))
                {
                    return OperationResult.CreateSuccess($"Source '{name}' removed successfully");
                }

                return OperationResult.CreateError($"Failed to remove source. Output: {output}");
            }
            catch (Exception ex)
            {
                return OperationResult.CreateError($"Failed to remove source: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates/refreshes a package source
        /// </summary>
        public async Task<OperationResult> UpdateSourceAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return OperationResult.CreateError("Source name is required");

                string arguments = $"source update --name \"{name}\"";
                
                string output;
                if (IsRunningAsAdmin())
                {
                    output = await ExecuteWingetCommandAsync(arguments);
                }
                else
                {
                    output = await ExecuteWingetCommandElevatedAsync(arguments);
                }

                if (output.Contains("successfully") || output.Contains("updated"))
                {
                    return OperationResult.CreateSuccess($"Source '{name}' updated successfully");
                }

                return OperationResult.CreateError($"Failed to update source. Output: {output}");
            }
            catch (Exception ex)
            {
                return OperationResult.CreateError($"Failed to update source: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets a package source to default settings
        /// </summary>
        public async Task<OperationResult> ResetSourceAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return OperationResult.CreateError("Source name is required");

                string arguments = $"source reset --name \"{name}\"";
                
                string output;
                if (IsRunningAsAdmin())
                {
                    output = await ExecuteWingetCommandAsync(arguments);
                }
                else
                {
                    output = await ExecuteWingetCommandElevatedAsync(arguments);
                }

                if (output.Contains("successfully") || output.Contains("reset"))
                {
                    return OperationResult.CreateSuccess($"Source '{name}' reset successfully");
                }

                return OperationResult.CreateError($"Failed to reset source. Output: {output}");
            }
            catch (Exception ex)
            {
                return OperationResult.CreateError($"Failed to reset source: {ex.Message}");
            }
        }

        private string ResolveWingetPath()
        {
            // Try to find full path in LocalAppData (common for modern installs)
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string wingetPath = Path.Combine(localAppData, @"Microsoft\WindowsApps\winget.exe");
            if (File.Exists(wingetPath)) return wingetPath;

            // Fallback to searching in PATH
            return WINGET_PATH;
        }

        private bool IsRunningAsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private async Task<string> ExecuteWingetCommandElevatedAsync(string arguments)
        {
            return await Task.Run(() =>
            {
                string tempScript = Path.Combine(Path.GetTempPath(), $"winget_cmd_{Guid.NewGuid():N}.bat");
                string tempOutput = Path.Combine(Path.GetTempPath(), $"winget_out_{Guid.NewGuid():N}.txt");
                try
                {
                    string wingetPath = ResolveWingetPath();
                    // Create a batch file to handle quoting and redirection cleanly
                    // Use double quotes around paths and arguments
                    string scriptContent = $"@echo off\r\n\"{wingetPath}\" {arguments} > \"{tempOutput}\" 2>&1";
                    File.WriteAllText(tempScript, scriptContent);

                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{tempScript}\"",
                        Verb = "runas",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        process?.WaitForExit();
                        return File.Exists(tempOutput) ? File.ReadAllText(tempOutput) : "Error: Elevated output file not found.";
                    }
                }
                catch (Exception ex)
                {
                    return $"Elevation failed: {ex.Message}";
                }
                finally
                {
                    if (File.Exists(tempScript))
                    {
                        try { File.Delete(tempScript); } catch { /* ignore */ }
                    }
                    if (File.Exists(tempOutput))
                    {
                        try { File.Delete(tempOutput); } catch { /* ignore */ }
                    }
                }
            });
        }

        /// <summary>
        /// Parses winget source list output into PackageSource objects
        /// </summary>
        private List<PackageSource> ParseSourceList(string output)
        {
            var sources = new List<PackageSource>();

            if (string.IsNullOrEmpty(output))
                return sources;

            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            bool parsingData = false;
            int nameColStart = -1;
            int argColStart = -1;
            int typeColStart = -1;
            int explicitColStart = -1;
            int dataColStart = -1;
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Look for the header line
                if (line.Contains("Name") && line.Contains("Argument"))
                {
                    nameColStart = line.IndexOf("Name", StringComparison.OrdinalIgnoreCase);
                    argColStart = line.IndexOf("Argument", StringComparison.OrdinalIgnoreCase);
                    typeColStart = line.IndexOf("Type", StringComparison.OrdinalIgnoreCase);
                    explicitColStart = line.IndexOf("Explicit", StringComparison.OrdinalIgnoreCase);
                    dataColStart = line.IndexOf("Data", StringComparison.OrdinalIgnoreCase);
                    
                    parsingData = true;
                    continue;
                }

                if (!parsingData)
                    continue;

                // Skip separator lines
                if (line.StartsWith("---") || line.StartsWith("==="))
                    continue;

                // Extract columns
                string name = "";
                string argument = "";
                string type = "";
                string data = "";

                if (nameColStart >= 0 && argColStart > nameColStart)
                {
                    int nameLen = argColStart - nameColStart;
                    // Determine end of Argument column based on Type or Explicit or Data column
                    int nextColStart = -1;
                    if (typeColStart > argColStart) nextColStart = typeColStart;
                    else if (explicitColStart > argColStart) nextColStart = explicitColStart;
                    else if (dataColStart > argColStart) nextColStart = dataColStart;

                    int argLen = nextColStart > argColStart ? nextColStart - argColStart : line.Length - argColStart;

                    if (nameColStart < line.Length)
                        name = line.Substring(nameColStart, Math.Min(nameLen, line.Length - nameColStart)).Trim();
                    if (argColStart < line.Length)
                        argument = line.Substring(argColStart, Math.Min(argLen, line.Length - argColStart)).Trim();
                    
                    // Clean up truncated URLs or potential artifacts
                    if (argument.EndsWith("…"))
                        argument = argument.TrimEnd('…');

                    if (typeColStart > 0 && typeColStart < line.Length)
                    {
                        int nextAfterType = explicitColStart > typeColStart ? explicitColStart : (dataColStart > typeColStart ? dataColStart : -1);
                        int typeLen = nextAfterType > typeColStart ? nextAfterType - typeColStart : line.Length - typeColStart;
                        type = line.Substring(typeColStart, Math.Min(typeLen, line.Length - typeColStart)).Trim();
                    }
                    
                    if (dataColStart > 0 && dataColStart < line.Length)
                        data = line.Substring(dataColStart).Trim();
                }
                else
                {
                    // Fallback: split by multiple spaces
                    var parts = Regex.Split(line, @"\s{2,}").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    
                    if (parts.Length >= 1) name = parts[0].Trim();
                    if (parts.Length >= 2) argument = parts[1].Trim();
                    if (parts.Length >= 3) type = parts[2].Trim();
                    if (parts.Length >= 4) data = parts[3].Trim();
                }

                // Validate that we have at least name
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var source = new PackageSource
                {
                    Name = name,
                    Argument = string.IsNullOrWhiteSpace(argument) ? "" : argument,
                    Type = string.IsNullOrWhiteSpace(type) ? "Unknown" : type,
                    Data = string.IsNullOrWhiteSpace(data) ? "" : data,
                    Identifier = name,
                    IsDefault = name.Equals("winget", StringComparison.OrdinalIgnoreCase)
                };

                sources.Add(source);
            }

            return sources;
        }

        /// <summary>
        /// Executes a winget command and returns the output
        /// </summary>
        private async Task<string> ExecuteWingetCommandAsync(string arguments)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = WINGET_PATH,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        if (process == null)
                        {
                            return "Error: Could not start process.";
                        }
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        return string.IsNullOrEmpty(error) ? output : $"{output}\n{error}";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error executing winget: {ex.Message}");
                    throw;
                }
            });
        }

        /// <summary>
        /// Parses winget list output into Package objects
        /// </summary>
        private List<Package> ParsePackageList(string output, PackageStatus status)
        {
            var packages = new List<Package>();

            if (string.IsNullOrEmpty(output))
                return packages;

            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            // Skip header lines and parse package entries
            bool parsingData = false;
            int nameColStart = -1;
            int idColStart = -1;
            int versionColStart = -1;
            int sourceColStart = -1;
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Look for the header line that contains "Name", "Id", etc.
                if (line.Contains("Name") && line.Contains("Id"))
                {
                    // Find column positions from header
                    nameColStart = line.IndexOf("Name", StringComparison.OrdinalIgnoreCase);
                    idColStart = line.IndexOf("Id", StringComparison.OrdinalIgnoreCase);
                    versionColStart = line.IndexOf("Version", StringComparison.OrdinalIgnoreCase);
                    sourceColStart = line.IndexOf("Source", StringComparison.OrdinalIgnoreCase);
                    
                    parsingData = true;
                    continue;
                }

                if (!parsingData)
                    continue;

                // Skip separator lines
                if (line.StartsWith("---") || line.StartsWith("===") || line.StartsWith("- "))
                    continue;

                // Skip lines that appear to be part of progress indicators
                if (line.Contains("\\") || line.Contains("/") || line.Contains("|") || line.Trim().StartsWith("-"))
                    continue;

                // Extract columns based on positions or by splitting
                string name = "";
                string id = "";
                string version = "";
                string source = "winget";

                if (nameColStart >= 0 && idColStart > nameColStart)
                {
                    // Use column-based extraction
                    int nameLen = idColStart - nameColStart;
                    int idLen = versionColStart > idColStart ? versionColStart - idColStart : line.Length - idColStart;

                    if (nameColStart < line.Length)
                        name = line.Substring(nameColStart, Math.Min(nameLen, line.Length - nameColStart)).Trim();
                    if (idColStart < line.Length)
                        id = line.Substring(idColStart, Math.Min(idLen, line.Length - idColStart)).Trim();
                    
                    if (versionColStart > 0 && versionColStart < line.Length)
                    {
                        int verLen = sourceColStart > versionColStart ? sourceColStart - versionColStart : line.Length - versionColStart;
                        version = line.Substring(versionColStart, Math.Min(verLen, line.Length - versionColStart)).Trim();
                    }
                    
                    if (sourceColStart > 0 && sourceColStart < line.Length)
                        source = line.Substring(sourceColStart).Trim();
                }
                else
                {
                    // Fallback: split by multiple spaces
                    var parts = Regex.Split(line, @"\s{2,}").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    
                    if (parts.Length < 2)
                        continue;

                    name = parts[0].Trim();
                    id = parts[1].Trim();
                    version = parts.Length > 2 ? parts[2].Trim() : "Unknown";
                    source = parts.Length > 3 ? parts[3].Trim() : "winget";
                }

                // Validate that we have at least name and id
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
                    continue;

                var package = new Package
                {
                    Name = name,
                    Id = id,
                    Version = string.IsNullOrWhiteSpace(version) ? "Unknown" : version,
                    Status = status,
                    LastUpdated = DateTime.Now,
                    Source = source
                };

                packages.Add(package);
            }

            return packages;
        }

        /// <summary>
        /// Parses winget upgrade output into Package objects with available versions
        /// </summary>
        private List<Package> ParseUpgradeList(string output)
        {
            var packages = new List<Package>();

            if (string.IsNullOrEmpty(output))
                return packages;

            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            // Skip header lines and parse package entries
            bool parsingData = false;
            int nameColStart = -1;
            int idColStart = -1;
            int versionColStart = -1;
            int availableColStart = -1;
            int sourceColStart = -1;
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Look for the header line that contains "Name", "Id", "Version"
                if (line.Contains("Name") && line.Contains("Id"))
                {
                    // Find column positions from header
                    nameColStart = line.IndexOf("Name", StringComparison.OrdinalIgnoreCase);
                    idColStart = line.IndexOf("Id", StringComparison.OrdinalIgnoreCase);
                    versionColStart = line.IndexOf("Version", StringComparison.OrdinalIgnoreCase);
                    availableColStart = line.IndexOf("Available", StringComparison.OrdinalIgnoreCase);
                    sourceColStart = line.IndexOf("Source", StringComparison.OrdinalIgnoreCase);
                    
                    parsingData = true;
                    continue;
                }

                if (!parsingData)
                    continue;

                // Skip separator lines
                if (line.StartsWith("---") || line.StartsWith("===") || line.StartsWith("- "))
                    continue;

                // Skip lines that appear to be part of progress indicators
                if (line.Contains("\\") || line.Contains("/") || line.Contains("|") || line.Trim().StartsWith("-"))
                    continue;

                // Extract columns based on positions
                string name = "";
                string id = "";
                string version = "";
                string availableVersion = "";
                string source = "winget";

                if (nameColStart >= 0 && idColStart > nameColStart)
                {
                    // Use column-based extraction
                    int nameLen = idColStart - nameColStart;
                    int idLen = versionColStart > idColStart ? versionColStart - idColStart : line.Length - idColStart;

                    if (nameColStart < line.Length)
                        name = line.Substring(nameColStart, Math.Min(nameLen, line.Length - nameColStart)).Trim();
                    if (idColStart < line.Length)
                        id = line.Substring(idColStart, Math.Min(idLen, line.Length - idColStart)).Trim();
                    
                    if (versionColStart > 0 && versionColStart < line.Length)
                    {
                        int verLen = availableColStart > versionColStart ? availableColStart - versionColStart : line.Length - versionColStart;
                        version = line.Substring(versionColStart, Math.Min(verLen, line.Length - versionColStart)).Trim();
                    }
                    
                    if (availableColStart > 0 && availableColStart < line.Length)
                    {
                        int availLen = sourceColStart > availableColStart ? sourceColStart - availableColStart : line.Length - availableColStart;
                        availableVersion = line.Substring(availableColStart, Math.Min(availLen, line.Length - availableColStart)).Trim();
                    }
                    
                    if (sourceColStart > 0 && sourceColStart < line.Length)
                        source = line.Substring(sourceColStart).Trim();
                }
                else
                {
                    // Fallback: split by multiple spaces
                    var parts = Regex.Split(line, @"\s{2,}").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    
                    if (parts.Length < 2)
                        continue;

                    name = parts[0].Trim();
                    id = parts[1].Trim();
                    version = parts.Length > 2 ? parts[2].Trim() : "Unknown";
                    availableVersion = parts.Length > 3 ? parts[3].Trim() : "Unknown";
                    source = parts.Length > 4 ? parts[4].Trim() : "winget";
                }

                // Validate that we have at least name and id
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
                    continue;

                var package = new Package
                {
                    Name = name,
                    Id = id,
                    Version = string.IsNullOrWhiteSpace(version) ? "Unknown" : version,
                    AvailableVersion = string.IsNullOrWhiteSpace(availableVersion) ? version : availableVersion,
                    Status = PackageStatus.UpdateAvailable,
                    LastUpdated = DateTime.Now,
                    Source = source
                };

                packages.Add(package);
            }

            return packages;
        }
    }
}
