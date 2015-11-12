using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.Globalization;
using System.Speech.Synthesis;
using System.Windows.Forms;
using System.Xml;

namespace CloudIA
{


    public partial class Form1 : Form
    {
        SpeechRecognitionEngine recengine = new SpeechRecognitionEngine();
        SpeechSynthesizer synth = new SpeechSynthesizer();
        string[] AllUsuarios;
        string ActiveUser;
        string[] ActiveUserContacts;
        bool UserState = true;
        bool PasswordState = false;
        bool ContactState = false;

        public Form1()
        {
            //XmlWriter writer = XmlWriter.Create("ElXML.xml");


            InitializeComponent();
            AllUsuarios = LoadUsuarios();
            AddGrammar(AllUsuarios);
            richTextBox1.Text += "---LOG--- \n";

        }

        private void btnEnable_Click(object sender, EventArgs e)
        {
            try
            {
                recengine.RecognizeAsync(RecognizeMode.Multiple);
                btnDisable.Enabled = true;
                btnEnable.Enabled = false;
            }
            catch (Exception)
            {

                throw;
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // TEXT TO SPEECH
            var voices = synth.GetInstalledVoices();
            synth.SelectVoice(voices[1].VoiceInfo.Name);
            synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Child);
            synth.SetOutputToDefaultAudioDevice();
            //TextToSpeech("Hi, welcome to CYNTHIA, your personal assistant. Who are you?");

            // SPEECH TO TEXT
            recengine.SetInputToDefaultAudioDevice();
            recengine.SpeechRecognized += Recengine_SpeechRecognized;



        }


        private void Recengine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            #region Login & Contacts
            if (UserState == true)
            {
                foreach (string user in AllUsuarios)
                {
                    if (e.Result.Text == user)
                    {

                        ActiveUser = user;
                        richTextBox1.Text += "\n Trying to connect... \n";
                        AddGrammar(new string[] { CheckPassword(user) });
                        synth.Speak("Hello " + user + ", give me your password");
                        UserState = false;
                        PasswordState = true;
                    }
                }
            }

            if (PasswordState == true)
            {
                if (e.Result.Text == "Back")
                {
                    ActiveUser = null;
                    richTextBox1.Text += "\n Who are you? \n";
                    AddGrammar(AllUsuarios);
                    synth.Speak("It seems that i made i mistake. Who are you?");
                    UserState = true;
                    PasswordState = false;
                }
                if (e.Result.Text == CheckPassword(ActiveUser))
                {
                    PasswordState = false;
                    ContactState = true;
                    richTextBox1.Text += "\n Logged in as: " + ActiveUser + "\n";
                    AddGrammar(GetContacts(ActiveUser));
                    ActiveUserContacts = GetContacts(ActiveUser);
                    synth.Speak("You succesfully logged in as " + ActiveUser);
                    synth.Speak("What do you want?");
                    lblActiveUser.Text = "Active user: " + ActiveUser;
                }
            }

            if (ContactState == true)
            {
                if (e.Result.Text == "Logout")
                {
                    synth.Speak("Goodbye " + ActiveUser);
                    richTextBox1.Text += "\n The user " + ActiveUser + " logged out. \n";
                    AddGrammar(AllUsuarios);
                    lblActiveUser.Text = "";
                    ActiveUser = null;
                    UserState = true;
                    ContactState = false;
                }

                foreach (string contact in ActiveUserContacts)
                {
                    if (e.Result.Text == contact)
                    {
                        richTextBox1.Text += "\n Contact: " + contact + " --- Number: " + GetNumber(ActiveUser, contact) + "\n";
                        synth.Speak("The number of contact " + contact + " is: " + GetNumber(ActiveUser, contact));
                    }
                }
            }
            #endregion

            switch (e.Result.Text)
            {
                case "What time":
                    synth.Speak("The current time is " + DateTime.Now.ToString("HH:mm:ss tt"));
                    richTextBox1.Text += "\n" + DateTime.Now.ToString("HH:mm:ss tt") + "\n";
                    break;
                case "Thank you":
                    synth.Speak("I am happy to help!");
                    break;
                case "Hello Cynthia":
                    if (ActiveUser == null)
                    {
                        synth.Speak("Hello unkown user. I would like to know who you are.");
                    }
                    else { synth.Speak("Hello " + ActiveUser + "!"); }
                    break;
                case "Are you fine":
                    synth.Speak("I am fine, thank you. I hope i can help you in your daily workload. Now i can only give you contact numbers that you have saved, but in the future i will make your life easier");
                    break;
                case "How old":
                    synth.Speak("I am 5 days old. I am just a baby. Hahahaha.");
                    break;
                case "Cynthia be polite":
                    synth.Speak("Oooh i'm sorry. Hello teachers and classmates! I hope you're having a great time!");
                    break;
            }
        }

        private void btnDisable_Click(object sender, EventArgs e)
        {
            recengine.RecognizeAsyncStop();
            btnDisable.Enabled = false;
            btnEnable.Enabled = true;
        }

        void AddGrammar(string[] words)
        {
            string[] BaseAndNew = new string[words.Length + 7];
            for (int i = 0; i < words.Length; i++)
            {
                BaseAndNew[i] = words[i];
            }
            BaseAndNew[words.Length] = "What time";
            BaseAndNew[words.Length + 1] = "Back";
            BaseAndNew[words.Length + 2] = "Logout";
            BaseAndNew[words.Length + 3] = "Thank you";
            BaseAndNew[words.Length + 4] = "Hello Cynthia";
            BaseAndNew[words.Length + 5] = "Are you fine";
            BaseAndNew[words.Length + 6] = "How old";
            BaseAndNew[words.Length + 6] = "Cynthia be polite";
            recengine.UnloadAllGrammars();
            Choices comm = new Choices();
            comm.Add(BaseAndNew);
            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(comm);
            Grammar grammar = new Grammar(gb);
            recengine.LoadGrammarAsync(grammar);

        }

        void TextToSpeech(string text)
        {
            synth.Speak(text);
        }

        public void NuevoContacto(string usuario, string nombre, string numero)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("./ElXML.xml");
            string xpath = "//Usuario[@username = '" + usuario + "']";
            XmlNode nodo = doc.SelectSingleNode(xpath);
            
            XmlElement elem = doc.CreateElement("Contacto");
            elem.SetAttribute("numero", numero);
            elem.SetAttribute("name", nombre);
            nodo.AppendChild(elem);
            doc.Save("./ElXML.xml");
        }

        public void NuevoUsuario(string usuario, string pass)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("./ElXML.xml");

            XmlElement elem = doc.CreateElement("Usuario");
            elem.SetAttribute("password", pass);
            elem.SetAttribute("username", usuario);
            doc.DocumentElement.AppendChild(elem);
            doc.Save("./ElXML.xml");
        }

        public string[] LoadUsuarios()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("./ElXML.xml");
            XmlNodeList lista = doc.GetElementsByTagName("Usuario");

            string[] respuesta = new string[lista.Count];
            int i = 0;
            foreach (XmlNode nodo in lista)
            {
                respuesta[i] = nodo.Attributes["username"].Value;
                i += 1;
            }
            return respuesta;
        }

        public string CheckPassword(string usuario)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("./ElXML.xml");
            XmlNodeList lista = doc.SelectNodes("/Usuarios/Usuario");
            string password = null;
            foreach (XmlNode nodo in lista)
            {
                if (nodo.Attributes["username"].Value == usuario)
                {
                    password = nodo.Attributes["password"].Value;
                }
            }
            return password;
        }
 
        public string[] GetContacts(string usuario)
        {
            string[] respuesta = null;

            XmlDocument doc = new XmlDocument();
            doc.Load("./ElXML.xml");
            XmlNodeList lista = doc.SelectNodes("/Usuarios/Usuario");
            foreach (XmlNode nodo in lista)
            {
                if (nodo.Attributes["username"].Value == usuario)
                {
                    respuesta = new string[nodo.ChildNodes.Count];
                    int i = 0;
                    foreach (XmlNode contacto in nodo.ChildNodes)
                    {
                        respuesta[i] = contacto.Attributes["name"].Value;
                        i++;
                    }
                }
            }

            return respuesta;
        }

        public string GetNumber(string usuario, string contacto)
        {
            string respuesta = null;

            XmlDocument doc = new XmlDocument();
            doc.Load("./ElXML.xml");
            XmlNodeList lista = doc.SelectNodes("/Usuarios/Usuario");
            foreach (XmlNode nodo in lista)
            {
                if (nodo.Attributes["username"].Value == usuario)
                {
                    foreach (XmlNode elcontacto in nodo.ChildNodes)
                    {
                        if (elcontacto.Attributes["name"].Value == contacto)
                            respuesta = elcontacto.Attributes["numero"].Value;
                    }
                }
            }

            return respuesta;
        }
        
        private void btnAddUser_Click(object sender, EventArgs e)
        {
            string name = NewUserName.Text;
            string pass = NewUserPass.Text;
            bool CanCreate = true;

            foreach (string PastUser in AllUsuarios)
            {
                if (name == PastUser)
                {
                    synth.Speak("The username " + name + " is already in use.");
                    richTextBox1.Text += "\n The username " + name + " is already in use. \n";
                    CanCreate = false;
                }
            }

            if (name == "")
            {
                synth.Speak("Your have to choose a username.");
                richTextBox1.Text += "\n Your have to choose a username. \n";
                CanCreate = false;
            }
            if (pass == "")
            {
                synth.Speak("Your need a password");
                richTextBox1.Text += "\n You need a password. We recommend you to use words and numbers. \n";
                CanCreate = false;
            }
            if (CanCreate)
            {
                NuevoUsuario(name, pass);
                NuevoContacto(name, "Tatrateasdgfrarvcx", "1");
                NewUserName.Text = "";
                NewUserPass.Text = "";
                richTextBox1.Text += "\n Succesfully created the user " + name + "\n";
                synth.Speak("Succesfully created the user " + name);
                AllUsuarios = LoadUsuarios();
                AddGrammar(AllUsuarios);
            }
        }

        private void btnAddContact_Click(object sender, EventArgs e)
        {
            bool CanCreate = true;
            string name = NewContactName.Text;
            string number = NewContactNumber.Text;

            if (ActiveUser == null)
            {
                richTextBox1.Text += "\n No active user \n";
                synth.Speak("There is not an active user");
            }
            else
            {
                foreach (string PastContact in GetContacts(ActiveUser))
                {
                    if (name == PastContact)
                    {
                        synth.Speak("The contact " + name + " exists.");
                        richTextBox1.Text += "\n The contact " + name + " already exists. \n";
                        CanCreate = false;
                    }
                }

                if (name == "")
                {
                    synth.Speak("Your have to name your contact");
                    richTextBox1.Text += "\n Your have to name your contact. \n";
                    CanCreate = false;
                }
                if (number == "")
                {
                    synth.Speak("Your contact needs a valid number");
                    richTextBox1.Text += "\n Your contact neeeds a valid number. \n";
                    CanCreate = false;
                }
                if (CanCreate)
                {
                    NuevoContacto(ActiveUser, name, number);
                    NewContactName.Text = "";
                    NewContactNumber.Text = "";
                    richTextBox1.Text += "\n Succesfully created the contact " + name + "\n";
                    synth.Speak("Succesfully created the contact " + name);
                    AddGrammar(GetContacts(ActiveUser));
                    ActiveUserContacts = GetContacts(ActiveUser);
                }
            }
        }
    }



    
}
