using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using SynapseX.Properties;

namespace SynapseX.CrackerSussyAssets
{
    /// <summary>
    /// Interaction logic for ScriptEditor.xaml
    /// </summary>
    public partial class ScriptEditor : UserControl
    {
        private string TabDir;
        public TextEditor SelectedEditor;

        public ScriptEditor()
        {
            InitializeComponent();
        }

        private static TextEditor CreateNewEditor(string content)
        {
            var editor = new TextEditor
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(1),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                ShowLineNumbers = true,
                FontFamily = new FontFamily(Settings.Default.FontName),
                FontSize = Settings.Default.FontSize,
                Text = content
            };
            
            editor.TextArea.TextView.SetResourceReference(TextView.LinkTextForegroundBrushProperty, "PrimaryTextColor");
            editor.SetResourceReference(TextEditor.ForegroundProperty, "PrimaryTextColor");
            editor.SetResourceReference(TextEditor.LineNumbersForegroundProperty, "PrimaryTextColor");

            XmlReader xml = new XmlTextReader(File.OpenRead("bs_bin/syntax.xhsd"));
            editor.SyntaxHighlighting = HighlightingLoader.Load(xml, HighlightingManager.Instance);

            editor.TextChanged += delegate
            {
                var offset = editor.CaretOffset;
                var line = editor.Document.GetLineByOffset(offset);
                editor.ScrollToLine(line.LineNumber);
            };

            Settings.Default.PropertyChanged += (_, args) =>
            {
                switch (args.PropertyName)
                {
                    case "FontName":
                        editor.FontFamily = new FontFamily(Settings.Default.FontName);
                        break;

                    case "FontSize":
                        editor.FontSize = Settings.Default.FontSize;
                        break;
                }
            };

            return editor;
        }

        public void AddTab(string file)
        {
            var info = new FileInfo(file);
            var content = File.ReadAllText(file);

            var editor = CreateNewEditor(content);

            var item = new TabItem
            {
                Header = info.Name,
                Content = editor,
                Tag = file
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Focusable = false
            };

            var text = new TextBlock
            {
                Text = info.Name,
                Focusable = true,
            };

            var removeButton = new Button
            {
                Content = "\xE8BB",
                FontSize = 8,
                Width = 12,
                Height = 12,
                Margin = new Thickness(5, 0, 0, 0),
                Visibility = Visibility.Collapsed,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Background = null
            };

            text.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextColor");
            removeButton.SetResourceReference(Button.ForegroundProperty, "PrimaryTextColor");

            panel.Children.Add(text);
            panel.Children.Add(removeButton);

            removeButton.Click += delegate
            {
                if (Tab.Items.Count == 1) return;
                RemoveTab(item);
            };

            item.Header = panel;
            item.Height = 24;

            Tab.Items.Add(item);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            timer.Tick += delegate
            {
                Save(item);
            };

            Tab.SelectionChanged += delegate
            {
                removeButton.Visibility = item.IsSelected ? Visibility.Visible : Visibility.Collapsed;
                if (item.IsSelected)
                {
                    SelectedEditor = editor;
                    CurrentFileName = text.Text;
                }

                else Save(item);

                timer.IsEnabled = item.IsSelected;
                TabChanged?.Invoke(item, EventArgs.Empty);
            };

            editor.TextChanged += (o, args) => TextChanged?.Invoke(o, args);
        }

        public void CreateTab(string fileName, string content = "")
        {
            fileName = Path.Combine(TabDir, fileName);

            if (File.Exists(fileName))
            {
                Messages.ShowGenericErrorMessage("File already exists!");
                return;
            }
            File.WriteAllText(fileName, content);
            AddTab(fileName);
        }

        public void RemoveTab(TabItem item)
        {
            var file = (string)item.Tag;
            if (File.Exists(file))
                File.Delete(file);

            Tab.Items.Remove(item);
        }

        //public void EditTab(TabItem item, string name)
        //{
        //    var oldFileName = (string)item.Tag;

        //    if (File.Exists(oldFileName))
        //    {
        //        File.Move(oldFileName, name);
        //        item.Tag = name;
        //    }

        //    else
        //        Tab.Items.Remove(item);
        //}

        public void RefreshAllTabs(bool SaveAll = true)
        {
            if (SaveAll) SaveAllTabs();
            var files = Directory.GetFiles(TabDir);
            if (files.Length == 0)
            {
                File.WriteAllText(Path.Combine(TabDir, "Script 0.lua"), "");
                files = Directory.GetFiles(TabDir);
            }

            Tab.Items.Clear();
            foreach (var file in files)
                AddTab(file);
            Tab.SelectedIndex = 0;
        }

        public void Save(TabItem item)
        {
            var file = (string)item.Tag;
            if (!File.Exists(file)) return;
            var editor = (TextEditor)item.Content;
            File.WriteAllText(file, editor.Text);
        }

        public void SaveAllTabs()
        {
            foreach (TabItem item in Tab.Items)
            {
                Save(item);
            }
        }

        public event EventHandler TextChanged;
        public event EventHandler TabChanged;
        public string CurrentFileName = "";

        public void InitializeDirectory(string dir)
        {
            TabDir = dir;
            RefreshAllTabs(false);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var count = 0;
            var name = $"Script {count}.lua";
            while (File.Exists(Path.Combine(TabDir, name)))
            {
                count++;
                name = $"Script {count}.lua";
            }

            CreateTab(name);
        }
    }
}
