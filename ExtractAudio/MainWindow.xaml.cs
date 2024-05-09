using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace ExtractAudio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Queue<string> _filesToConvert = new Queue<string>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnSourceFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            txtSourceFolder.Text = GetFolderPath(txtSourceFolder.Text);
        }

        private string GetFolderPath(string originalPath)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
                return dialog.FileName;
            else
                return originalPath;
        }

        private void btnOutputFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            txtOutputFolder.Text = GetFolderPath(txtOutput.Text);
        }
        private static List<string> GetFilesToConvert(string directoryPath)
        {
            return Directory
                .EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                .Where(file => file.ToLower().EndsWith("mkv") || file.ToLower().EndsWith("mp4") || file.ToLower().EndsWith("m4v") || file.ToLower().EndsWith("avi"))
                .ToList();
        }
        private async void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            int? numberOfFiles = 0;
            int numberOfCompletedFiles = 0;

            try
            {
                if (!Directory.Exists(txtOutputFolder.Text))
                {
                    MessageBox.Show("Select a valid Output folder");
                    return;
                }

                if (!Directory.Exists(txtSourceFolder.Text))
                {
                    MessageBox.Show("Select a valid Source folder");
                    return;
                }

                if (btnStartStop.Content.ToString().Equals("Start"))
                {
                    btnStartStop.Content = "Stop";
                    txtOutput.Text = string.Empty;
                }
                else
                {
                    //TODO: find a way to cancel the in-process conversion, clearing the queue will prevent
                    //new conversions from starting
                    _filesToConvert.Clear();
                    btnStartStop.Content = "Start";
                    return;
                }

                txtOutput.Text = $"Getting latest FFmpeg version... {Environment.NewLine}{txtOutput.Text}";
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);

                _filesToConvert = new Queue<string>(GetFilesToConvert(txtSourceFolder.Text));
                numberOfFiles = _filesToConvert?.Count;

                int i = 0;
                while (_filesToConvert?.Count > 0)
                {
                    txtFileCount.Text = $"{++i} of {numberOfFiles}";
                    string file = _filesToConvert.Dequeue();
                    string outputFileName = Path.Combine(txtOutputFolder.Text, Path.GetFileName(Path.ChangeExtension(file, ".mp3")));

                    if (File.Exists(outputFileName))
                    {
                        txtOutput.Text = $"Skipping this file because its MP3 output already exists: {file} {Environment.NewLine}{txtOutput.Text}";
                        continue;
                    }

                    txtOutput.Text = $"Converting: {file} {Environment.NewLine}{txtOutput.Text}";
                    try
                    {
                        var conversion = await FFmpeg.Conversions.FromSnippet.ExtractAudio(file, outputFileName);
                        await conversion.Start().ContinueWith(t =>
                        {
                            ++numberOfCompletedFiles;
                            Dispatcher.Invoke(() =>
                            {
                                txtOutput.Text = $"Finished converting: {file} {Environment.NewLine}{txtOutput.Text}";
                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            txtOutput.Text = $"Error converting: {file} {Environment.NewLine} \t {ex.Message} {Environment.NewLine}{txtOutput.Text}";
                        });
                    }
                }

                btnStartStop.Content = "Start";
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    txtOutput.Text = $"Error: {ex.Message} {Environment.NewLine}{txtOutput.Text}";
                });
            }
            finally
            {
                txtOutput.Text = $"Finished extracting audio from {numberOfCompletedFiles} files. {Environment.NewLine}{txtOutput.Text}";
            }
        }
    }
}
