namespace WhatsOn.mock.app
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.IO.File.Copy("SubtitleRequirements.xml", "C:\\temp\\translationrequest\\SubtitleRequirements.xml");
        }
    }
}