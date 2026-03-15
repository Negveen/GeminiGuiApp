using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeminiGuiApp
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Очистить кэш". 
        /// Асинхронно запускает процесс физического удаления временных файлов, 
        /// чтобы интерфейс программы не "зависал" во время очистки, 
        /// после чего уведомляет пользователя о завершении операции.
        /// </summary>
        private async void btnClearCache_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
        "Это безвозвратно удалит все несохраненные черновики сессий из памяти Gemini CLI.\nВаши сохраненные Воркспейсы затронуты не будут.\n\nПродолжить?",
        "Очистка кэша",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                btnClearCache.Enabled = false;
                btnClearCache.Text = "Удаление...";

                await System.Threading.Tasks.Task.Run(() => WipeGeminiTraces());

                btnClearCache.Text = "Очистить кэш CLI (Удалить черновики)";
                btnClearCache.Enabled = true;
                MessageBox.Show("Временные файлы CLI успешно удалены!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// Физически удаляет системный мусор (временные файлы, логи и старые нативные сессии), 
        /// сгенерированный утилитой Gemini CLI в папке ~/.gemini/tmp. 
        /// Безопасно игнорирует файлы, которые в данный момент заняты другими процессами.
        /// </summary>
        private void WipeGeminiTraces()
        {
            // Главная папка .gemini
            string geminiRoot = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini");

            string tmpDir = System.IO.Path.Combine(geminiRoot, "tmp");
            string historyDir = System.IO.Path.Combine(geminiRoot, "history");

            // 1. УМНАЯ ОЧИСТКА TMP (Всё кроме bin)
            if (System.IO.Directory.Exists(tmpDir))
            {
                // Удаляем файлы в корне tmp
                foreach (string file in System.IO.Directory.GetFiles(tmpDir))
                {
                    try { System.IO.File.Delete(file); } catch { /* Игнорируем занятые файлы */ }
                }

                // Удаляем папки в корне tmp, кроме bin
                foreach (string dir in System.IO.Directory.GetDirectories(tmpDir))
                {
                    try
                    {
                        if (System.IO.Path.GetFileName(dir).ToLower() != "bin")
                        {
                            System.IO.Directory.Delete(dir, true);
                        }
                    }
                    catch { /* Игнорируем занятые папки */ }
                }
            }

            // 2. БЕЗЖАЛОСТНОЕ УДАЛЕНИЕ ИСТОРИИ CLI
            if (System.IO.Directory.Exists(historyDir))
            {
                try
                {
                    System.IO.Directory.Delete(historyDir, true);
                }
                catch { /* Игнорируем, если занята */ }
            }
            // На всякий случай, если history - это файл, а не папка (как бывает в bash)
            else if (System.IO.File.Exists(historyDir))
            {
                try { System.IO.File.Delete(historyDir); } catch { }
            }
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            chkShiftEnter.Checked = Form1.SendWithShiftEnter;
        }

        private void chkShiftEnter_CheckedChanged(object sender, EventArgs e)
        {
            Form1.SendWithShiftEnter = chkShiftEnter.Checked;
        }
    }
}
