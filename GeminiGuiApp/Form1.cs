using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace GeminiGuiApp
{
    public partial class Form1 : Form
    {
        // Переменная для хранения пути к выбранному файлу        
        private string _selectedPath = string.Empty;
        private bool _isFileSelected = false; // Флаг, чтобы понимать, файл это или папка

        public Form1()
        {
            InitializeComponent();
        }

        // 1. Логика кнопки выбора файла
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedPath = openFileDialog.FileName;
                    _isFileSelected = true;
                    lblSelectedPath.Text = $"Файл: {_selectedPath}";
                    lblSelectedPath.ForeColor = Color.Green;
                }
            }
        }

        // 2. Логика кнопки отправки запроса
        private async void btnSend_Click(object sender, EventArgs e)
        {
            string userPrompt = txtPrompt.Text.Trim();
            if (string.IsNullOrEmpty(userPrompt)) return;

            txtPrompt.Clear();

            // Печатаем ваш запрос и подготавливаем строку для ответа
            rtbOutput.AppendText($"[Вы]: {userPrompt}\n");
            rtbOutput.AppendText($"[Gemini]:\n");

            btnSend.Enabled = false;

            try
            {
                // Создаем обработчик, который безопасно обновляет интерфейс
                var progress = new Progress<string>(text =>
                {
                    rtbOutput.AppendText(text + "\n");

                    // Автопрокрутка в самый низ, чтобы видеть новый текст
                    rtbOutput.SelectionStart = rtbOutput.Text.Length;
                    rtbOutput.ScrollToCaret();
                });

                // Запускаем наш новый метод (обратите внимание, мы передаем progress)
                await RunGeminiCliStreamingAsync(userPrompt, progress);

                rtbOutput.AppendText("\n\n"); // Отступ после завершения ответа
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSend.Enabled = true;
            }
        }

        // 3. Метод-"обертка" для запуска консольной утилиты
        private System.Threading.Tasks.Task RunGeminiCliStreamingAsync(string prompt, IProgress<string> progress)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";

                // 1. Санитизация и контекст (всё как было)
                string safePrompt = prompt.Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
                string workingDir = string.Empty;
                string contextInstruction = string.Empty;

                if (!string.IsNullOrEmpty(_selectedPath))
                {
                    if (_isFileSelected)
                    {
                        workingDir = System.IO.Path.GetDirectoryName(_selectedPath);
                        string fileName = System.IO.Path.GetFileName(_selectedPath);
                        contextInstruction = $" [Системно: Сфокусируйся на работе с файлом '{fileName}']";
                    }
                    else
                    {
                        workingDir = _selectedPath;
                    }
                }

                safePrompt += contextInstruction;
                string command = $"gemini \"{safePrompt}\" --yolo";
                process.StartInfo.Arguments = $"/C \"{command}\"";

                if (!string.IsNullOrEmpty(workingDir) && System.IO.Directory.Exists(workingDir))
                {
                    process.StartInfo.WorkingDirectory = workingDir;
                }

                // 2. Настройки процесса
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

                // 3. ПОДПИСЫВАЕМСЯ НА СОБЫТИЯ ПОТОКОВОГО ВЫВОДА
                process.OutputDataReceived += (sender, args) =>
                {
                    // Если строка не пустая, отправляем её в наш интерфейс
                    if (args.Data != null)
                    {
                        progress.Report(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    // Логи об ошибках выводим сереньким или просто помечаем
                    if (args.Data != null)
                    {
                        progress.Report($"(Log: {args.Data})");
                    }
                };

                // 4. Запускаем процесс
                process.Start();

                // 5. НАЧИНАЕМ АСИНХРОННОЕ ЧТЕНИЕ (Вместо ReadToEnd)
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 6. Ждем, пока процесс закончит свою работу
                process.WaitForExit();
            });
        }
        

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedPath = folderDialog.SelectedPath;
                    _isFileSelected = false;
                    lblSelectedPath.Text = $"Папка: {_selectedPath}";
                    lblSelectedPath.ForeColor = Color.Blue;
                }
            }
        }
    }
}