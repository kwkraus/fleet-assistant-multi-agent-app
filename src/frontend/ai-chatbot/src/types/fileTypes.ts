// File upload types for the chat interface

export interface FileAttachment {
  id: string;
  file: File;
  name: string;
  size: number;
  type: string;
  preview?: string; // For image previews
  uploadProgress?: number;
  error?: string;
}

export interface FileUploadError {
  code: string;
  message: string;
  fileName?: string;
}

export const SUPPORTED_FILE_TYPES = {
  // Documents
  'application/pdf': ['.pdf'],
  'application/msword': ['.doc'],
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx'],
  'text/plain': ['.txt'],
  'text/csv': ['.csv'],
  
  // Spreadsheets
  'application/vnd.ms-excel': ['.xls'],
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': ['.xlsx'],
  
  // Images
  'image/jpeg': ['.jpg', '.jpeg'],
  'image/png': ['.png'],
  'image/gif': ['.gif'],
  'image/webp': ['.webp']
} as const;

export const MAX_FILE_SIZE = 20 * 1024 * 1024; // 20MB
export const MAX_FILES_PER_MESSAGE = 5;

export const getFileIcon = (type: string): string => {
  if (type.startsWith('image/')) return '🖼️';
  if (type === 'application/pdf') return '📄';
  if (type.includes('word')) return '📝';
  if (type.includes('excel') || type.includes('spreadsheet') || type === 'text/csv') return '📊';
  if (type === 'text/plain') return '📋';
  return '📎';
};

export const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

export const isFileTypeSupported = (type: string): boolean => {
  return Object.keys(SUPPORTED_FILE_TYPES).includes(type);
};

export const validateFile = (file: File): FileUploadError | null => {
  // Check file size
  if (file.size > MAX_FILE_SIZE) {
    return {
      code: 'FILE_TOO_LARGE',
      message: `File size must be less than ${formatFileSize(MAX_FILE_SIZE)}`,
      fileName: file.name
    };
  }

  // Check file type
  if (!isFileTypeSupported(file.type)) {
    const supportedExtensions = Object.values(SUPPORTED_FILE_TYPES).flat();
    return {
      code: 'UNSUPPORTED_FILE_TYPE',
      message: `File type not supported. Supported types: ${supportedExtensions.join(', ')}`,
      fileName: file.name
    };
  }

  return null;
};
