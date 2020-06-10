using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebBrowserCorruptionTest
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

                for (int i = 0; i < 3; i++) // change to test different number of threads
                {
                    var thread = new Thread(UIThread);
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                }

                const string Html = "<html><head><title>Title</title></head><body>go!</body></html>";
                var file = TempFile.Create(Encoding.UTF8.GetBytes(Html));

                var browser = new WebBrowser();
                browser.Dock = DockStyle.Fill;
                var form = new Form();
                form.Controls.Add(browser);
                form.Load += delegate { browser.Navigate(file.Path); };

                Application.Run(form);

                GC.KeepAlive(file);

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Environment.FailFast("Test Code Panic", ex);
            }
        }

        private static void UIThread(object obj)
        {
            var frame = new ApplicationContext();
            Application.Idle += delegate { RunTest(); };
            Application.Run(frame);
        }

        private static void RunTest()
        {
            try
            {
                const string Html = "<html><head><title>Title</title></head></html>";

                using (var file = TempFile.Create(Encoding.UTF8.GetBytes(Html)))
                using (var parent = new Control())
                using (var control = new WebBrowser { Parent = parent })
                {
                    bool completed = false;
                    control.DocumentCompleted += (sender, e) => completed = true;
                    control.Navigate(file.Path);

                    while (!completed)
                        Application.DoEvents();

                    System.Diagnostics.Debug.Assert(control.Document != null);
                }
            }
            catch (Exception ex)
            {
                Environment.FailFast("Error in Test Code", ex);
            }
        }
    }

    public sealed class TempFile : IDisposable
    {
        public string Path { get; }

        public TempFile(string path, byte[] data)
        {
            Path = path;

            if (data != null)
                File.WriteAllBytes(path, data);
        }

        ~TempFile() => DeleteFile();

        public static TempFile Create(byte[] bytes)
        {
            return new TempFile(System.IO.Path.GetTempFileName(), bytes);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            DeleteFile();
        }

        private void DeleteFile()
        {
            try
            {
                File.Delete(Path);
            }
            catch
            {
            }
        }
    }
}
