# Markdown Rendering Components

This document outlines the markdown components supported in the AI chat assistant's conversation bubbles.

## Supported Markdown Elements

### Headers
- **H1**: Large headers for main titles
- **H2**: Medium headers for section titles  
- **H3**: Small headers for subsections
- **H4**: Smaller headers for sub-subsections
- **H5**: Very small headers
- **H6**: Smallest headers

### Lists
- **Unordered Lists**: Bullet point lists with proper spacing
- **Ordered Lists**: Numbered lists with proper spacing
- **List Items**: Individual list items with consistent styling

### Text Formatting
- **Bold Text**: `**text**` or `__text__`
- **Italic Text**: `*text*` or `_text_`
- **Inline Code**: `code` with background highlighting
- **Code Blocks**: Multi-line code with syntax highlighting

### Tables
- **Full Table Support**: Complete table rendering with:
  - Table headers (thead) with gray background
  - Table body (tbody) with row striping
  - Bordered cells with proper padding
  - Responsive horizontal scrolling
  - Support for markdown formatting within cells (bold, italic, etc.)

### Blockquotes
- **Blockquotes**: Indented quotes with left border styling

### Paragraphs
- **Paragraphs**: Properly spaced text blocks with responsive font sizing

## Copy Functionality

When users click the "Copy" button on assistant messages, they receive the original markdown-formatted text, preserving:
- Table structure
- Header formatting
- List formatting
- Bold/italic formatting
- Code formatting
- All other markdown syntax

## Example Usage

```markdown
# Fleet Status Report

## Current Vehicle Status

| Vehicle ID | Status | Fuel Level | Location |
|------------|--------|------------|----------|
| VH001      | **Active** | 75%    | Route A  |
| VH002      | *Maintenance* | 45% | Depot    |

### Key Metrics:
- **Total Vehicles**: 4
- **Active Vehicles**: 3

> **Note**: This data is updated in real-time.
```

## Technical Implementation

- Uses `react-markdown` with `remark-gfm` plugin for GitHub Flavored Markdown support
- Custom component styling with Tailwind CSS classes
- Responsive design with mobile-first approach
- Accessibility-compliant table structures
- Dark mode support through CSS variables