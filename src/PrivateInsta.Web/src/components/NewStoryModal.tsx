import { useState, useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import { XMarkIcon, PhotoIcon } from '@heroicons/react/24/outline';
import { useQueryClient } from '@tanstack/react-query';
import { stories as storiesApi } from '../api';

export function NewStoryModal({ onClose }: { onClose: () => void }) {
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const qc = useQueryClient();

  const onDrop = useCallback((accepted: File[]) => {
    const f = accepted[0];
    if (!f) return;
    setFile(f);
    setPreview(prev => { if (prev) URL.revokeObjectURL(prev); return URL.createObjectURL(f); });
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { 'image/*': [], 'video/*': [] },
    maxFiles: 1,
  });

  const handleSubmit = async () => {
    if (!file) return;
    setUploading(true);
    try {
      const form = new FormData();
      form.append('file', file);
      await storiesApi.create(form);
      qc.invalidateQueries({ queryKey: ['stories'] });
      onClose();
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl w-full max-w-sm shadow-2xl">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="font-semibold text-gray-900">New Story</h2>
          <button onClick={onClose}><XMarkIcon className="w-5 h-5 text-gray-500" /></button>
        </div>

        <div className="p-4 space-y-4">
          {!preview
            ? <div {...getRootProps()} className={`border-2 border-dashed rounded-xl p-10 text-center cursor-pointer ${isDragActive ? 'border-pink-400 bg-pink-50' : 'border-gray-300'}`}>
                <input {...getInputProps()} />
                <PhotoIcon className="w-10 h-10 mx-auto text-gray-300 mb-2" />
                <p className="text-sm text-gray-500">Drop a photo or video</p>
              </div>
            : <div className="relative rounded-xl overflow-hidden">
                {file?.type.startsWith('video/')
                  ? <video src={preview} className="w-full max-h-64 object-cover" controls />
                  : <img src={preview} alt="preview" className="w-full max-h-64 object-cover" />}
                <button onClick={() => { setFile(null); setPreview(p => { if (p) URL.revokeObjectURL(p); return null; }); }} className="absolute top-2 right-2 bg-black/50 text-white rounded-full p-1">
                  <XMarkIcon className="w-4 h-4" />
                </button>
              </div>
          }
          <p className="text-xs text-gray-400 text-center">Stories disappear after 24 hours</p>
        </div>

        <div className="p-4 border-t flex justify-end">
          <button
            onClick={handleSubmit}
            disabled={!file || uploading}
            className="bg-pink-600 text-white text-sm font-semibold px-6 py-2 rounded-full disabled:opacity-40 hover:bg-pink-700"
          >
            {uploading ? 'Uploading…' : 'Share Story'}
          </button>
        </div>
      </div>
    </div>
  );
}
