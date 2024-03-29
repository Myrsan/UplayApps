﻿using System.Net.Sockets;
using System.Text;
using UbiServices.Records;
using static UbiServices.Public.V3;

namespace CoreLib
{
    public class LoginLib
    {
        public static LoginJson? LoginFromStore(string[] args, int index)
        {
            var logins = LoginStore.Load();
            if (index >= logins.Count)
                return TryLoginWithArgsCLI(args);
            return LoginFromStore(args, logins[index].UserId);
        }
        public static LoginJson? LoginFromStore(string[] args,string UserId)
        {
            LoginJson? login = null;
            var logins = LoginStore.Load();
            var loginUser = logins.Where(x => x.UserId == UserId);
            if (loginUser.Any())
            {
                var loginFirst = loginUser.First();
                if (!string.IsNullOrEmpty(loginFirst.RemDeviceTicket))
                    login = LoginRememberDevice(loginFirst.RemTicket, loginFirst.RemDeviceTicket);
                if (!string.IsNullOrEmpty(loginFirst.RemTicket))
                    login = LoginRemember(loginFirst.RemTicket);
            }
            else
            {
                args = args.Append("-savedevice").ToArray();
                TryLoginWithArgsCLI(args);
            }
            if (login != null)
            {
                LoginStore.FromLogin(login);
            }
            return login;
        }

        public static LoginJson? TryLoginWithArgsCLI(string[] args)
        {
            LoginJson? login = null;
            if (ParameterLib.HasParameter(args, "-b64"))
            {
                var b64 = ParameterLib.GetParameter<string>(args, "-b64");
                login = LoginBase64(b64);
            }
            else if ((ParameterLib.HasParameter(args, "-username") || ParameterLib.HasParameter(args, "-user")) && (ParameterLib.HasParameter(args, "-password") || ParameterLib.HasParameter(args, "-pass")))
            {
                var username = ParameterLib.GetParameter<string>(args, "-username") ?? ParameterLib.GetParameter<string>(args, "-user");
                var password = ParameterLib.GetParameter<string>(args, "-password") ?? ParameterLib.GetParameter<string>(args, "-pass");
                login = Login(username, password);
            }
            else
            {
                Console.WriteLine("Please enter your Email:");
                string username = Console.ReadLine()!;
                Console.WriteLine("Please enter your Password:");
                string password = ReadPassword();
                login = Login(username, password);
            }
            if (login.Ticket == null)
            {
                Console.WriteLine("Your account has 2FA, please enter your code:");
                var code2fa = Console.ReadLine();
                if (code2fa == null)
                {
                    Console.WriteLine("Code cannot be empty!");
                    return null;
                }
                if (ParameterLib.HasParameter(args, "-savedevice"))
                {
                    string trustedname = ParameterLib.GetParameter(args, "-trustedname", Environment.MachineName);
                    string trustedid = ParameterLib.GetParameter(args, "-trustedid", GenerateDeviceId(trustedname));
                    login = TryLoginWith2FA_Rem(login, trustedname, trustedid);
                }
                else
                {
                    login = TryLoginWith2FA(login, code2fa);
                }
            }
            if (login != null)
            {
                LoginStore.FromLogin(login);
            }
            return login;
        }

        public static LoginJson? TryLoginWithArgsCLI_RemTicket(string[] args, out bool Had2FA)
        {
            Had2FA = false;
            Console.WriteLine(string.Join(", ", args));
            LoginJson? login = null;

            if (ParameterLib.HasParameter(args, "-ref"))
            {
                var refticket = ParameterLib.GetParameter<string>(args, "-ref");
                login = LoginRemember(refticket);
                Console.WriteLine(login);
            }
            else if (ParameterLib.HasParameter(args, "-b64"))
            {
                var b64 = ParameterLib.GetParameter<string>(args, "-b64");
                if (ParameterLib.HasParameter(args, "-rem"))
                {
                    Console.WriteLine("HAS REM!");
                    var remticket = ParameterLib.GetParameter<string>(args, "-rem");
                    login = LoginB64Device(b64, remticket);
                }
                else
                {
                    login = LoginBase64(b64);
                }
            }
            else if ((ParameterLib.HasParameter(args, "-username") || ParameterLib.HasParameter(args, "-user")) && (ParameterLib.HasParameter(args, "-password") || ParameterLib.HasParameter(args, "-pass")))
            {
                var username = ParameterLib.GetParameter<string>(args, "-username") ?? ParameterLib.GetParameter<string>(args, "-user");
                var password = ParameterLib.GetParameter<string>(args, "-password") ?? ParameterLib.GetParameter<string>(args, "-pass");
                login = Login(username, password);
            }
            else
            {
                Console.WriteLine("Please enter your Email:");
                string username = Console.ReadLine()!;
                Console.WriteLine("Please enter your Password:");
                string password = ReadPassword();
                login = Login(username, password);
            }
            if (login.Ticket == null)
            {
                Had2FA = true;
                Console.WriteLine("Your account has 2FA, please enter your code:");
                var code2fa = Console.ReadLine();
                if (code2fa == null)
                {
                    Console.WriteLine("Code cannot be empty!");
                    return null;
                }
                if (ParameterLib.HasParameter(args, "-trustedname"))
                {
                    string trustedname = ParameterLib.GetParameter(args, "-trustedname", Environment.MachineName);
                    string trustedid = ParameterLib.GetParameter(args, "-trustedid", GenerateDeviceId(trustedname));
                    login = TryLoginWith2FA_Rem(login, code2fa, trustedname, trustedid);
                }
                else
                {
                    login = TryLoginWith2FA(login, code2fa);
                }
            }
            if (login != null)
            {
                LoginStore.FromLogin(login);
            }
            return login;
        }

        public static LoginJson? TryLoginWithArgs(string[] args)
        {
            LoginJson? login = null;
            if (ParameterLib.HasParameter(args, "-b64"))
            {
                var b64 = ParameterLib.GetParameter<string>(args, "-b64");
                login = LoginBase64(b64);
            }
            else if ((ParameterLib.HasParameter(args, "-username") || ParameterLib.HasParameter(args, "-user")) && (ParameterLib.HasParameter(args, "-password") || ParameterLib.HasParameter(args, "-pass")))
            {
                var username = ParameterLib.GetParameter<string>(args, "-username") ?? ParameterLib.GetParameter<string>(args, "-user");
                var password = ParameterLib.GetParameter<string>(args, "-password") ?? ParameterLib.GetParameter<string>(args, "-pass");
                login = Login(username, password);
            }
            if (login != null)
            {
                LoginStore.FromLogin(login);
            }
            return login;
        }

        public static LoginJson? TryLoginWith2FA_Rem(LoginJson? login, string code2fa, string trustedname)
        {
            var deviceId = GenerateDeviceId(trustedname);
            return TryLoginWith2FA_Rem(login, code2fa, trustedname, deviceId);
        }

        public static LoginJson? TryLoginWith2FA_Rem(LoginJson? login, string code2fa, string trustedname, string trustedId)
        {
            LoginJson? ret = login;
            Console.WriteLine(login.ToString());
            if (login.Ticket == null && login.TwoFactorAuthenticationTicket != null)
            {
                ret = Login2FA_Device(login.TwoFactorAuthenticationTicket, code2fa, trustedId,trustedname);
            }
            Console.WriteLine(ret.ToString());
            return ret;
        }

        public static LoginJson? TryLoginWith2FA(LoginJson? login, string code2fa)
        {
            LoginJson? ret = login;
            if (login.Ticket == null && login.TwoFactorAuthenticationTicket != null)
            {
                ret = Login2FA(login.TwoFactorAuthenticationTicket, code2fa);
            }
            return ret;
        }

        //  Thanks from SteamRE!
        public static string ReadPassword()
        {
            ConsoleKeyInfo keyInfo;
            var password = new StringBuilder();

            do
            {
                keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }

                    continue;
                }
                /* Printable ASCII characters only */
                var c = keyInfo.KeyChar;
                if (c >= ' ' && c <= '~')
                {
                    password.Append(c);
                    Console.Write('*');
                }
            } while (keyInfo.Key != ConsoleKey.Enter);

            return password.ToString();
        }
    }
}