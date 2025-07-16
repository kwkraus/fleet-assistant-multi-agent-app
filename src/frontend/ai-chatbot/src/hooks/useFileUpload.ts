"use client"

import { useState, useCallback } from 'react'
import { FileAttachment, validateFile, FileUploadError, MAX_FILES_PER_MESSAGE, fileToBase64 } from '../types/fileTypes'

interface UseFileUploadOptions {
  maxFiles?: number
  onError?: (error: FileUploadError) => void
}

export function useFileUpload({ maxFiles = MAX_FILES_PER_MESSAGE, onError }: UseFileUploadOptions = {}) {
  const [attachments, setAttachments] = useState<FileAttachment[]>([])
  const [isDragActive, setIsDragActive] = useState(false)

  const addFiles = useCallback((files: File[]) => {
    const newAttachments: FileAttachment[] = []
    
    for (const file of files) {
      // Check if we're at the max file limit
      if (attachments.length + newAttachments.length >= maxFiles) {
        onError?.({
          code: 'TOO_MANY_FILES',
          message: `Maximum ${maxFiles} files allowed per message`
        })
        break
      }

      // Validate file
      const error = validateFile(file)
      if (error) {
        onError?.(error)
        continue
      }

      // Check for duplicates
      if (attachments.some(att => att.name === file.name && att.size === file.size)) {
        onError?.({
          code: 'DUPLICATE_FILE',
          message: `File "${file.name}" is already attached`,
          fileName: file.name
        })
        continue
      }

      // Create attachment
      const attachment: FileAttachment = {
        id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        file,
        name: file.name,
        size: file.size,
        type: file.type
      }

      // Generate base64 data for API requests
      fileToBase64(file).then(base64Data => {
        setAttachments(prev => 
          prev.map(att => 
            att.id === attachment.id 
              ? { ...att, base64Data }
              : att
          )
        )
      }).catch(error => {
        console.error('Error converting file to base64:', error)
        onError?.({
          code: 'BASE64_CONVERSION_ERROR',
          message: `Failed to process file: ${file.name}`,
          fileName: file.name
        })
      })

      // Generate preview for images
      if (file.type.startsWith('image/')) {
        const reader = new FileReader()
        reader.onload = (e) => {
          setAttachments(prev => 
            prev.map(att => 
              att.id === attachment.id 
                ? { ...att, preview: e.target?.result as string }
                : att
            )
          )
        }
        reader.readAsDataURL(file)
      }

      newAttachments.push(attachment)
    }

    if (newAttachments.length > 0) {
      setAttachments(prev => [...prev, ...newAttachments])
    }
  }, [attachments, maxFiles, onError])

  const removeFile = useCallback((id: string) => {
    setAttachments(prev => prev.filter(att => att.id !== id))
  }, [])

  const clearFiles = useCallback(() => {
    setAttachments([])
  }, [])

  const handleDragEnter = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragActive(true)
  }, [])

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    
    // Only set drag inactive if we're leaving the actual drop zone
    if (!e.currentTarget.contains(e.relatedTarget as Node)) {
      setIsDragActive(false)
    }
  }, [])

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
  }, [])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragActive(false)

    const files = Array.from(e.dataTransfer.files)
    if (files.length > 0) {
      addFiles(files)
    }
  }, [addFiles])

  const handleFileInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || [])
    if (files.length > 0) {
      addFiles(files)
    }
    // Reset input value to allow selecting the same file again
    e.target.value = ''
  }, [addFiles])

  return {
    attachments,
    isDragActive,
    addFiles,
    removeFile,
    clearFiles,
    dragHandlers: {
      onDragEnter: handleDragEnter,
      onDragLeave: handleDragLeave,
      onDragOver: handleDragOver,
      onDrop: handleDrop
    },
    handleFileInputChange,
    hasFiles: attachments.length > 0,
    fileCount: attachments.length,
    canAddMoreFiles: attachments.length < maxFiles
  }
}
