using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;

public class StickyApp : Form {
    private TabControl notebook;
    private string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "notes_data.xml");

    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int HT_CAPTION = 0x2;

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    public StickyApp() {
        try { if (File.Exists("app.ico")) this.Icon = new Icon("app.ico"); } catch { }
        this.FormBorderStyle = FormBorderStyle.None;
        this.TopMost = true;
        this.Size = new Size(400, 450);
        this.BackColor = Color.FromArgb(241, 224, 90);
        this.Opacity = 0.95;
        this.Text = "Sticky Notes";

        Panel titleBar = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = Color.FromArgb(241, 224, 90) };
        Label lblTitle = new Label { Text = "Sticky Notes", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(80, 80, 80) };
        
        Button btnClose = new Button { Text = "X", Dock = DockStyle.Right, Width = 35, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold) };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (s, e) => Application.Exit();

        Button btnMin = new Button { Text = "_", Dock = DockStyle.Right, Width = 35, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold) };
        btnMin.FlatAppearance.BorderSize = 0;
        btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
        
        Button btnAdd = new Button { Text = "+", Dock = DockStyle.Left, Width = 35, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 12, FontStyle.Bold) };
        btnAdd.FlatAppearance.BorderSize = 0;
        btnAdd.Click += (s, e) => AddNewTab("Note " + (notebook.TabCount + 1), "");

        titleBar.Controls.Add(btnMin);
        titleBar.Controls.Add(btnClose);
        titleBar.Controls.Add(btnAdd);
        titleBar.Controls.Add(lblTitle); 
        this.Controls.Add(titleBar);
        lblTitle.MouseDown += MoveForm;
        titleBar.MouseDown += MoveForm;

        Panel mainContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5, 40, 5, 5) };
        this.Controls.Add(mainContainer);
        notebook = new TabControl { Dock = DockStyle.Fill, HotTrack = true };
        mainContainer.Controls.Add(notebook);
        LoadData();
    }

    private void MoveForm(object sender, MouseEventArgs e) {
        if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); }
    }

    private void AddNewTab(string title, string content) {
        TabPage tp = new TabPage(title);
        tp.BackColor = Color.FromArgb(255, 247, 173);
        RichTextBox rtb = new RichTextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(255, 247, 173), Font = new Font("Segoe UI", 11) };

        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Gras (Ctrl+B)", null, (s, e) => ToggleStyle(rtb, FontStyle.Bold));
        menu.Items.Add("Italique (Ctrl+I)", null, (s, e) => ToggleStyle(rtb, FontStyle.Italic));
        menu.Items.Add("Souligné (Ctrl+U)", null, (s, e) => ToggleStyle(rtb, FontStyle.Underline));
        menu.Items.Add(new ToolStripSeparator());
        
        // NOUVELLES OPTIONS DE TAILLE SUR LA SÉLECTION
        menu.Items.Add("Agrandir la sélection", null, (s, e) => ChangeFontSize(rtb, 2));
        menu.Items.Add("Réduire la sélection", null, (s, e) => ChangeFontSize(rtb, -2));
        
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Couleur du texte", null, (s, e) => ChangeTextColor(rtb));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Renommer cet onglet", null, (s, e) => RenameTab());
        menu.Items.Add("Supprimer cet onglet", null, (s, e) => DeleteTab());
        
        rtb.ContextMenuStrip = menu;
        rtb.KeyDown += (s, e) => {
            if (e.Control && e.KeyCode == Keys.B) { ToggleStyle(rtb, FontStyle.Bold); e.SuppressKeyPress = true; }
            if (e.Control && e.KeyCode == Keys.I) { ToggleStyle(rtb, FontStyle.Italic); e.SuppressKeyPress = true; }
            if (e.Control && e.KeyCode == Keys.U) { ToggleStyle(rtb, FontStyle.Underline); e.SuppressKeyPress = true; }
            // Raccourcis pour la taille : Ctrl + '+' ou '-'
            if (e.Control && e.KeyCode == Keys.Add) { ChangeFontSize(rtb, 2); e.SuppressKeyPress = true; }
            if (e.Control && e.KeyCode == Keys.Subtract) { ChangeFontSize(rtb, -2); e.SuppressKeyPress = true; }
        };

        if (!string.IsNullOrEmpty(content) && content.StartsWith("{\\rtf"))
            try { rtb.Rtf = content; } catch { rtb.Text = content; }
        else rtb.Text = content;

        rtb.TextChanged += (s, e) => SaveData();
        tp.Controls.Add(rtb);
        notebook.TabPages.Add(tp);
        notebook.SelectedTab = tp;
    }

    // MÉTHODE POUR CHANGER LA TAILLE DU TEXTE SÉLECTIONNÉ
    private void ChangeFontSize(RichTextBox rtb, float delta) {
        if (rtb.SelectionFont == null) return;
        float newSize = rtb.SelectionFont.Size + delta;
        if (newSize < 5 || newSize > 100) return; // Limites de sécurité
        rtb.SelectionFont = new Font(rtb.SelectionFont.FontFamily, newSize, rtb.SelectionFont.Style);
        SaveData();
    }

    private void ToggleStyle(RichTextBox rtb, FontStyle style) {
        if (rtb.SelectionFont == null) return;
        rtb.SelectionFont = new Font(rtb.SelectionFont, rtb.SelectionFont.Style ^ style);
        SaveData();
    }

    private void ChangeTextColor(RichTextBox rtb) {
        ColorDialog cd = new ColorDialog();
        if (cd.ShowDialog() == DialogResult.OK) { rtb.SelectionColor = cd.Color; SaveData(); }
    }

    private void RenameTab() {
        if (notebook.SelectedTab == null) return;
        Form prompt = new Form() { Width = 300, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog, Text = "Renommer", StartPosition = FormStartPosition.CenterParent, TopMost = true };
        Label lbl = new Label() { Left = 20, Top = 20, Text = "Nouveau nom :", Width = 250 };
        TextBox txt = new TextBox() { Left = 20, Top = 45, Width = 240, Text = notebook.SelectedTab.Text };
        Button ok = new Button() { Text = "OK", Left = 185, Width = 75, Top = 80, DialogResult = DialogResult.OK };
        prompt.Controls.Add(lbl); prompt.Controls.Add(txt); prompt.Controls.Add(ok);
        prompt.AcceptButton = ok;
        if (prompt.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(txt.Text)) { notebook.SelectedTab.Text = txt.Text; SaveData(); }
    }

    private void DeleteTab() {
        if (notebook.SelectedTab == null || notebook.TabPages.Count <= 1) return;
        if (MessageBox.Show("Supprimer l'onglet '" + notebook.SelectedTab.Text + "' ?", "Confirmer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
            notebook.TabPages.Remove(notebook.SelectedTab);
            SaveData();
        }
    }

    private void SaveData() {
        try {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("StickyConfig");
            foreach (TabPage tp in notebook.TabPages) {
                XmlElement note = doc.CreateElement("Note");
                note.SetAttribute("Title", tp.Text);
                note.InnerText = ((RichTextBox)tp.Controls[0]).Rtf;
                root.AppendChild(note);
            }
            doc.AppendChild(root);
            doc.Save(filePath);
        } catch {}
    }

    private void LoadData() {
        if (File.Exists(filePath)) {
            try {
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);
                foreach (XmlNode node in doc.SelectNodes("//Note")) { AddNewTab(node.Attributes["Title"].Value, node.InnerText); }
            } catch { AddNewTab("Note 1", ""); }
        }
        if (notebook.TabCount == 0) AddNewTab("Note 1", "");
    }

    [STAThread]
    static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new StickyApp());
    }
}