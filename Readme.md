# Doc2Md  
**A lightweight CLI tool to convert DOCX, PDF, and HTML files into Markdown.**

Doc2Md is a command‑line utility written in C# that processes individual files or entire folders, converting supported formats into clean Markdown (`.md`).  
It supports **DOCX**, **PDF**, and **HTML/HTM** input files, with optional extraction of images and assets.

---

## ✨ Features

### **DOCX → Markdown**
- Uses **Mammoth** to convert `.docx` files into HTML, then transforms the HTML into Markdown.
- Extracts embedded images and saves them into a dedicated folder.
- Produces GitHub‑flavored Markdown.

### **PDF → Markdown (Structured Heuristics)**
- Extracts text using **PdfPig** and reconstructs structure heuristically:
  - Detects headings (H1–H3) based on relative font size.
  - Detects bold/italic text.
  - Attempts to detect lists and indentation levels.
  - Attempts to detect table rows based on spacing.
- **Limitations (important):**
  - PDF structure is *inferred*, not explicit — results may be imperfect.
  - Tables may not be reproduced accurately.
  - **Images and multimedia are not extracted or saved.**

### **HTML → Markdown**
- Loads HTML via **HtmlAgilityPack**.
- Cleans the document (removes `<script>` and `<style>`).
- Attempts to download and rewrite references to:
  - Images (`<img src="...">`)
  - Stylesheets (`<link rel="stylesheet">`)
  - JavaScript files (if referenced)
- Converts the cleaned HTML to Markdown using ReverseMarkdown.
- Saves assets into a dedicated folder and rewrites relative paths.

### **Folder Support**
You can pass an entire directory as input.  
Doc2Md will scan it and convert all supported files inside it.

---

## 🚀 Usage

```
Doc2Md <input-file-or-folder> [output-file-or-folder] [options]
```

### **Parameters**

#### **`<input-file-or-folder>` (required)**
- A single file (`.docx`, `.pdf`, `.html`, `.htm`)
- OR a directory containing supported files.

#### **`[output-file-or-folder]` (optional)**
- For single-file input:  
  - If omitted, output is created next to the input file with `.md` extension.
- For directory input:  
  - If omitted, Markdown files are written into the same directory.

#### **Options**

| Option | Description |
|--------|-------------|
| `-m <path>`<br>`--media-dir <path>` | Base directory where extracted images/assets will be stored. If omitted, a folder is created next to the output `.md` file. |
| `-h`<br>`--help` | Shows the help menu. |

### **Examples**

#### Convert a single DOCX file
```
Doc2Md report.docx
```

#### Convert a PDF and specify output path
```
Doc2Md input.pdf output.md
```

#### Convert a folder of mixed files
```
Doc2Md ./documents ./markdown-output
```

#### Convert HTML and store assets in a custom folder
```
Doc2Md page.html output.md --media-dir ./assets
```

---

## 📁 Output Structure

Depending on the input file, Doc2Md may create additional folders:

- `filename_images/` → extracted DOCX images  
- `filename_assets/` → downloaded HTML images, CSS, JS  

Markdown files reference these assets using **relative paths**.

---

## ⚠️ PDF Conversion Notes

PDFs do **not** contain semantic structure.  
This tool reconstructs structure using heuristics:

- Headings are guessed from font size.
- Lists are detected from indentation and bullet characters.
- Tables are detected from spacing between words.

Because of this:

- Heading levels may be wrong.
- Lists may not nest correctly.
- Tables may be poorly reconstructed.
- **Images and multimedia are not extracted.**

Despite these limitations, the output is usually readable and significantly better than plain text extraction.

---

## 🛠️ Development Story (How This Tool Was Built)

This project was created through a hybrid workflow combining **Google Antigravity**, **Microsoft Copilot** and , **GitHub Copilot**:

1. **Initial prototype (DOCX only)**  
   I generated a first working version using **Google Antigravity**, focused solely on converting DOCX files.  
   The prototype worked but included unnecessary features, so I manually cleaned and simplified it.

2. **Interactive expansion with Microsoft Copilot**  
   Using Copilot in chatbot mode, I iteratively added:
   - PDF structured extraction  
   - HTML conversion  
   - Asset downloading  
   - Directory processing  
   - Code cleanup and comments  

3. **Final polishing**  
   Copilot helped refine the code, improve readability, and generate this GitHub‑ready README.

4. **Complete refactor and reorganization**  
   I was not satisfied with the result in terms of code cleanliness and modularization to make it more readable and maintainable; I therefore asked GitHub Copilot for a complete refactor with dependency injection, cleaner namespaces, and organized folders. After the refactor I obtained the final result and updated the README.md.

### ⏱️ Time Saved Thanks to AI

- **Actual time spent:** less than 2 hours  
- **Estimated time without AI:**  
  - 4 hours to research libraries and approaches  
  - 1 full day to implement  
  - ~1 day for debugging and cleanup  

Although there are still some improvements to make (XML-format code documentation, more concise and optimized code, and removal of any repetition), the result is already remarkable.
