using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Bokrondellen.ImportUtility
{
    public class AccountData
    {
        private int _id;
        [XmlElement("id")]
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _typ;
        [XmlElement("typ")]
        public string Typ
        {
            get { return _typ; }
            set { _typ = value; }
        }

        private string _refId;
        [XmlElement("ref_id")]
        public string RefId
        {
            get { return _refId; }
            set { _refId = value; }
        }

        private string _namn;
        [XmlElement("namn")]
        public string Namn
        {
            get { return _namn; }
            set { _namn = value; }
        }

        private int avtalRef;
        [XmlElement("avtal_ref")]
        public int AvtalRef
        {
            get { return avtalRef; }
            set { avtalRef = value; }
        }

        private string _adress1;
        [XmlElement("adress1")]
        public string Address1
        {
            get { return _adress1; }
            set { _adress1 = value; }
        }

        private string _adress2;
        [XmlElement("adress2")]
        public string Address2
        {
            get { return _adress2; }
            set { _adress2 = value; }
        }

        private string _adress3;
        [XmlElement("adress3")]
        public string Address3
        {
            get { return _adress3; }
            set { _adress3 = value; }
        }

        private string _postnummer;
        [XmlElement("postnummer")]
        public string Postnummer
        {
            get { return _postnummer; }
            set { _postnummer = value; }
        }
        private string _ort;
        [XmlElement("ort")]
        public string Ort
        {
            get { return _ort; }
            set { _ort = value; }
        }        
        
        private string _lastLogin;
        [XmlElement("last_login")]
        public string LastLogin
        {
            get { return _lastLogin; }
            set { _lastLogin = value; }
        }

        private int _loginId;
        [XmlElement("login_id")]
        public int LoginId
        {
            get { return _loginId; }
            set { _loginId = value; }
        }

        private string _username;
        [XmlElement("username")]
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        private string _password;
        [XmlElement("password")]
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }  


    }
}
