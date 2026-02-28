# GeminiGuiApp

Этот проект использует Gemini CLI. Ниже приведены инструкции по его установке.

## Установка Gemini CLI

### 1. Глобальная установка через npm (рекомендуется)
Это стандартный способ установки CLI для большинства пользователей:
```bash
npm install -g @google/gemini-cli
```

### 2. Использование Homebrew (macOS/Linux)
Если вы используете Homebrew, вы можете установить его с помощью:
```bash
brew install gemini-cli
```

### 3. Использование MacPorts (macOS)
Для пользователей MacPorts:
```bash
sudo port install gemini-cli
```

### 4. Использование Anaconda
Для изолированных сред вы можете создать специальную среду с Node.js, а затем установить CLI:
```bash
conda create -y -n gemini_env -c conda-forge nodejs
conda activate gemini_env
npm install -g @google/gemini-cli
```

### Альтернатива: Запуск без установки (npx)
Вы можете мгновенно запустить Gemini CLI без постоянной установки с помощью `npx`:
```bash
npx @google/gemini-cli
```

### Каналы выпусков
Вы можете выбирать между различными версиями выпусков, добавляя теги к команде npm:
- **Стабильная (по умолчанию):** `npm install -g @google/gemini-cli`
- **Preview:** `npm install -g @google/gemini-cli@preview` (обновляется еженедельно)
- **Nightly:** `npm install -g @google/gemini-cli@nightly` (обновляется ежедневно)

**Примечание:** Gemini CLI предустановлен в Google Cloud Shell и Cloud Workstations.

### Системные требования
- **Среда выполнения:** Node.js 20.0.0+
- **ОС:** macOS 15+, Windows 11 24H2+ или Ubuntu 20.04+
- **Оборудование:** 4 ГБ+ ОЗУ (рекомендуется 16 ГБ+ для активного использования)
- **Оболочка:** Bash или Zsh
