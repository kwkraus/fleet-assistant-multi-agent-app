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
  base64Data?: string; // Base64 encoded file data for API requests
}

export interface FileUploadError {
  code: string;
  message: string;
  fileName?: string;
}

// Interface for API requests - matches backend Base64File model
export interface Base64File {
  name: string;
  type: string;
  size: number;
  content: string; // Base64 encoded content
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

export const MAX_FILE_SIZE = 3 * 1024 * 1024; // 3MB to match backend default
export const MAX_FILES_PER_MESSAGE = 2; // 2 files to match backend limit

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

// Utility function to convert File to base64
export const fileToBase64 = (file: File): Promise<string> => {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result === 'string') {
        // Remove the data URL prefix (e.g., "data:image/png;base64,")
        const base64 = reader.result.split(',')[1];
        resolve(base64);
      } else {
        reject(new Error('Failed to read file as base64'));
      }
    };
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
};

// Convert FileAttachment to Base64File for API requests
export const fileAttachmentToBase64File = async (attachment: FileAttachment): Promise<Base64File> => {
  if (attachment.base64Data) {
    return {
      name: attachment.name,
      type: attachment.type,
      size: attachment.size,
      content: attachment.base64Data
    };
  }
  
  const base64Data = await fileToBase64(attachment.file);
  return {
    name: attachment.name,
    type: attachment.type,
    size: attachment.size,
    content: base64Data
  };
};
