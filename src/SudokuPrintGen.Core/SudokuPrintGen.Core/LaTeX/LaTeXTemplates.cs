using System.Text;
using SudokuPrintGen.Core.Puzzle;

namespace SudokuPrintGen.Core.LaTeX;

/// <summary>
/// Loads and processes LaTeX templates from files.
/// </summary>
public static class LaTeXTemplates
{
    /// <summary>
    /// Gets the templates directory path.
    /// </summary>
    private static string GetTemplatesPath()
    {
        // Try multiple possible locations
        var possiblePaths = new[]
        {
            // Relative to executable
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "latex"),
            // Relative to project root (for development)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "templates", "latex"),
            // Absolute from workspace
            Path.Combine("templates", "latex"),
            // From current working directory
            Path.Combine(Directory.GetCurrentDirectory(), "templates", "latex")
        };
        
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }
        
        // Return default relative path
        return Path.Combine("templates", "latex");
    }
    
    /// <summary>
    /// Loads a template file and replaces variables.
    /// </summary>
    public static string LoadTemplate(string templateName, Dictionary<string, string> variables)
    {
        var templatesPath = GetTemplatesPath();
        var templatePath = Path.Combine(templatesPath, templateName);
        
        // Try to load from templates folder
        if (File.Exists(templatePath))
        {
            var template = File.ReadAllText(templatePath);
            return ReplaceVariables(template, variables);
        }
        
        // If template not found, return empty string (will use inline fallback)
        return string.Empty;
    }
    
    private static string ReplaceVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace(key, value);
        }
        return result;
    }
    
    /// <summary>
    /// Gets the document header with all setup.
    /// </summary>
    public static string GetDocumentHeader(LaTeXStyleOptions options, GeneratedPuzzle? metadata = null)
    {
        var variables = new Dictionary<string, string>
        {
            ["FONT_SIZE"] = options.FontSize,
            ["TITLE"] = "", // No title
            ["AUTHOR"] = "", // No author
            ["DATE"] = "" // No date
        };
        
        // Engine-specific packages
        var enginePackages = new StringBuilder();
        if (options.Engine == LaTeXEngine.XeLaTeX)
        {
            enginePackages.AppendLine(@"\usepackage{fontspec}");
            if (!string.IsNullOrEmpty(options.FontFamily))
            {
                enginePackages.AppendLine(@"\setmainfont{" + options.FontFamily + "}");
            }
        }
        else if (!string.IsNullOrEmpty(options.FontFamily))
        {
            enginePackages.AppendLine(@"\usepackage{" + options.FontFamily + "}");
        }
        variables["ENGINE_PACKAGES"] = enginePackages.ToString();
        
        // Font setup
        variables["FONT_SETUP"] = string.Empty; // Already handled in ENGINE_PACKAGES
        
        // Color definitions (if needed for variants)
        var colorDefs = new StringBuilder();
        if (metadata?.Variant == Variant.ColorConstrained || metadata?.Variant == Variant.Kikagaku)
        {
            colorDefs.AppendLine(@"\definecolor{sred}{HTML}{FAB3BA}");
            colorDefs.AppendLine(@"\definecolor{sviolet}{HTML}{EDD4FF}");
            colorDefs.AppendLine(@"\definecolor{sgrey}{HTML}{DFDDD8}");
            colorDefs.AppendLine(@"\definecolor{sorange}{HTML}{F3CE82}");
            colorDefs.AppendLine(@"\definecolor{spink}{HTML}{F1A1DC}");
            colorDefs.AppendLine(@"\definecolor{syellow}{HTML}{F6FC7B}");
            colorDefs.AppendLine(@"\definecolor{slgreen}{HTML}{C8FBAE}");
            colorDefs.AppendLine(@"\definecolor{sdgreen}{HTML}{99EECD}");
            colorDefs.AppendLine(@"\definecolor{sblue}{HTML}{A4DFF2}");
        }
        variables["COLOR_DEFINITIONS"] = colorDefs.ToString();
        
        var header = LoadTemplate("document-header.tex", variables);
        if (string.IsNullOrEmpty(header))
        {
            // Fallback to inline generation
            return GenerateDocumentHeaderInline(options, metadata);
        }
        
        return header;
    }
    
    private static string GenerateDocumentHeaderInline(LaTeXStyleOptions options, GeneratedPuzzle? metadata)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"\documentclass[" + options.FontSize + @"]{article}");
        sb.AppendLine(@"\usepackage{xcolor}");
        sb.AppendLine(@"\usepackage{geometry}");
        sb.AppendLine(@"\usepackage{calc}");
        // Optimized margins for letter paper (8.5" × 11")
        sb.AppendLine(@"\geometry{letterpaper,");
        sb.AppendLine(@"            left=0.75in, right=0.75in,");
        sb.AppendLine(@"            top=0.5in, bottom=0.5in,");
        sb.AppendLine(@"            headheight=0pt, headsep=0pt,");
        sb.AppendLine(@"            footskip=0pt}");
        
        if (options.Engine == LaTeXEngine.XeLaTeX)
        {
            sb.AppendLine(@"\usepackage{fontspec}");
            if (!string.IsNullOrEmpty(options.FontFamily))
            {
                sb.AppendLine(@"\setmainfont{" + options.FontFamily + "}");
            }
        }
        
        // No title, author, date - start puzzles immediately
        sb.AppendLine(@"\begin{document}");
        sb.AppendLine(@"\pagestyle{empty}");
        return sb.ToString();
    }
}

