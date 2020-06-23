using System;
using System.Windows.Forms;

namespace CoreCorruptionTest2
{
    static class Program
    {
        static volatile bool shutdown;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new Form();
            form.FormClosing += delegate { shutdown = true; Application.Exit(); };
            form.Load += delegate
            {
                for (int i = 0; i < 2; i++)
                {
                    var thread = new System.Threading.Thread(RunUI);
                    thread.TrySetApartmentState(System.Threading.ApartmentState.STA);
                    thread.Start();
                }
            };

            Application.Run(form);
        }

        private static void RunUI()
        {
            using (var host = new Control())
            {
                Action loop = null;
                loop = delegate
                {
                    TestMethod(true);
                    TestMethod(false);

                    if (shutdown)
                        Application.ExitThread();
                    else
                        host.BeginInvoke(loop);
                };

                host.CreateControl();
                host.BeginInvoke(loop);

                Application.Run();
            }
        }

        private static void TestMethod(bool useCompatibleStateImageBehavior)
        {
            using (var imageList = new ImageList())
            using (var listView = new ListView
            {
                UseCompatibleStateImageBehavior = useCompatibleStateImageBehavior,
                View = View.SmallIcon,
                StateImageList = imageList,
            })
            {
                listView.CreateControl();
                listView.CheckBoxes = true;
                listView.CheckBoxes = false;
            }
        }
    }
}
