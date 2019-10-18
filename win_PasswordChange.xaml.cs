using Sentry;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace RfReader_demo
{
    /// <summary>
    /// Interaction logic for win_PasswordChange.xaml
    /// </summary>
    public partial class win_PasswordChange : Window
    {
        public static string conn = ConfigurationManager.ConnectionStrings["DSN"].ConnectionString;
        public win_PasswordChange()
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    InitializeComponent();
                    lbl_StatusMessage.Visibility = Visibility.Hidden;
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                }
            }
        }
      
        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    lbl_StatusMessage.Content = "";
                    lbl_StatusMessage.Visibility = Visibility.Hidden;
                    var oldPassword = txtbx_OldPassword.Password;
                    var newPassword = txtbx_NewPassword.Password;
                    var confirmPassword = txtbx_ConfirmPassword.Password;

                    if (oldPassword.Length < 1 || newPassword.Length < 1 || confirmPassword.Length < 1)
                    {
                        lbl_StatusMessage.Content = "Please fill out the fields.";
                        lbl_StatusMessage.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        var Status_OldPassword = CheckOldPassword(oldPassword);
                        if (Status_OldPassword == true)
                        {
                            if (newPassword == confirmPassword)
                            {
                                var Status_PasswordChanged = PasswordChanged(newPassword);
                                if (Status_PasswordChanged == true)
                                {
                                    lbl_StatusMessage.Content = "Password changed successfully.";
                                    lbl_StatusMessage.Visibility = Visibility.Visible;
                                    lbl_StatusMessage.Background = new SolidColorBrush(Colors.LightGreen);
                                    lbl_StatusMessage.Foreground = new SolidColorBrush(Colors.Green);
                                    lbl_StatusMessage.BorderBrush = new SolidColorBrush(Colors.Green);
                                    lbl_StatusMessage.BorderThickness = new Thickness(2.0);
                                    MessageBox.Show("Password changed successfully", "Password Changed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    this.Close();
                                }
                                else
                                {
                                    lbl_StatusMessage.Content = "Some error occured on password changing. Please try again.";
                                    lbl_StatusMessage.Visibility = Visibility.Visible;
                                }
                            }
                            else
                            {
                                lbl_StatusMessage.Content = "New Password and Confirm Password is not same.";
                                lbl_StatusMessage.Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            lbl_StatusMessage.Content = "You entered wrong old password.";
                            lbl_StatusMessage.Visibility = Visibility.Visible;
                        }
                    }
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                }
            }
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    this.Close();
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                }
            }
        }
        private void btn_Quit_Click(object sender, RoutedEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    this.Close();
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                }
            }
        }
        private bool CheckOldPassword(string pass)
        {
            using (SentrySdk.Init(conn))
            {
                string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
                string fileName = System.IO.Path.Combine(path, "LoginCredential.xml");

                try
                {
                    DataSet ds = new DataSet();
                    ds.ReadXml(fileName);

                    var dt = ds.Tables["LoginCredential"];
                    if (dt != null)
                    {
                        var getUserName_XML = dt.Rows[0][0].ToString();
                        var getPass_XML = dt.Rows[0][1].ToString();
                        var getUserName_Decrypt = Helper.Crypto.Decrypt(getUserName_XML);
                        var getPass_Decrypt = Helper.Crypto.Decrypt(getPass_XML);

                        if (pass == getPass_Decrypt)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                    return false;
                }
            }
        }

        private bool PasswordChanged(string newPassword)
        {
            using (SentrySdk.Init(conn))
            {
                try
                {
                    var username = "admin";
                    var password = newPassword;
                    string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
                    string fileName = System.IO.Path.Combine(path, "LoginCredential.xml");

                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                    xmlWriterSettings.Indent = true;
                    xmlWriterSettings.NewLineOnAttributes = true;
                    using (XmlWriter xmlWriter = XmlWriter.Create(fileName, xmlWriterSettings))
                    {
                        xmlWriter.WriteStartDocument();
                        /*LoginCredential Config*/
                        xmlWriter.WriteStartElement("LoginCredential");
                        xmlWriter.WriteElementString("UserName", Helper.Crypto.Encrypt(username));
                        xmlWriter.WriteElementString("Password", Helper.Crypto.Encrypt(password));
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteEndDocument();
                        xmlWriter.Flush();
                        xmlWriter.Close();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                    return false;
                }
            }
        }        
    }
}
