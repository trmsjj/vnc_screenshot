using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using VncSharp;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace vnc_screenshot
{
    class Program
    {
        static Bitmap Framebuffer = null;
        static bool GotFrame = false;

        static void Main(string[] args)
        {
            //pass the help
            if(args.Length == 1)
                if (args[0] == "?" || args[0] == "/?" || args[0] == "/h")
                    Console.Write("vnc_screenshot \"i:[IP Address]\" \"f:[IP Address List File]\" \"p:[Password]\" o:[Output Directory]");

            //parse the arguments
            string ip_address = "";
            string file_list = "";
            string password = "";
            string output_dir = "";
            foreach (string arg in args)
            {
                if(arg.ToLower().StartsWith("i:"))
                    ip_address = arg.Substring(2, arg.Length - 2);
                if(arg.ToLower().StartsWith("f:"))
                    file_list = arg.Substring(2, arg.Length - 2);
                if(arg.ToLower().StartsWith("p:"))
                    password = arg.Substring(2, arg.Length - 2);
                if (arg.ToLower().StartsWith("o:"))
                    output_dir = arg.Substring(2, arg.Length - 2);
            }


            //get the ip address
            if (ip_address.Length > 0)
            {
                GetFrame(ip_address, password, output_dir);
            }

            //get the text file
            if (file_list.Length != 0)
            {
                if (File.Exists(file_list))
                {
                    StreamReader sr = null;
                    sr = File.OpenText(file_list);

                    while (!sr.EndOfStream)
                    {
                        ip_address = sr.ReadLine();
                        string tmp_pw = password;
                        if(ip_address.Contains(":"))
                        {
                            tmp_pw = ip_address.Split(':')[1];
                            ip_address = ip_address.Split(':')[0];
                        }
                        GetFrame(ip_address, tmp_pw, output_dir);
                    }

                    if (sr != null)
                        sr.Close();
                }
                else
                    Console.Write("Invalid input file\n\r");
            }
                
        }

        static void GetFrame(string ip_address, string password, string output_dir)
        {
            GotFrame = false;

            try
            {
                VncClient vnc = new VncClient();
                vnc.Connect(ip_address);
                vnc.Authenticate(password);
                vnc.Initialize();
                vnc.StartUpdates();

                Framebuffer = new Bitmap(vnc.Framebuffer.Width, vnc.Framebuffer.Height, PixelFormat.Format32bppPArgb);
                vnc.VncUpdate += new VncUpdateHandler(vnc_VncUpdate);

                DateTime FrameGrabTimeout = DateTime.Now.AddSeconds(30);
                while (!GotFrame)
                {
                    if (DateTime.Now > FrameGrabTimeout)
                        break;
                    Thread.Sleep(500);
                }

                vnc.Disconnect();

                if (output_dir.Length == 0)
                    output_dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

                DirectoryInfo dir = new DirectoryInfo(output_dir);
                dir.Create();

                Framebuffer.Save(dir.FullName + "\\" + ip_address + ".png", ImageFormat.Png);
            }
            catch (ObjectDisposedException s)
            {
                Console.Write("ERROR authenticating to " + ip_address + ".\n\r");
                return;
            }
            catch (Exception e)
            {
                Console.Write("ERROR contacting " + ip_address + ". " + e.Message + "\n\r");
                return;
            }

            Console.Write("Captured " + ip_address + "\n\r");
        }

        static void vnc_VncUpdate(object sender, VncEventArgs e)
        {
            if (Framebuffer != null)
            {
                e.DesktopUpdater.Draw(Framebuffer);
                GotFrame = true;
            }
        }


        private static string GetPassword()
        {
           return "super-secret-password";
        }
    }
}
