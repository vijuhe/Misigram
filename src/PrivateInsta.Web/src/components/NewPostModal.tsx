import { useState, useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import { XMarkIcon, PhotoIcon } from '@heroicons/react/24/outline';
import { useQueryClient } from '@tanstack/react-query';
import { posts as postsApi } from '../api';

export function NewPostModal({ onClose }: { onClose: () => void }) {
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [caption, setCaption] = useState('');
  const [uploading, setUploading] = useState(false);
  const qc = useQueryClient();

  const onDrop = useCallback((accepted: File[]) => {
    const f = accepted[0];
    if (!f) return;
    setFile(f);
    setPreview(URL.createObjectURL(f));
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { 'image/*': [], 'video/*': [] },
    maxFiles: 1,
  });

  const canSubmit = !uploading && (!!file || caption.trim().length > 0);

  const handleSubmit = async () => {
    if (!canSubmit) return;
    setUploading(true);
    try {
      const form = new FormData();
      if (file) form.append('file', file);
      if (caption) form.append('caption', caption);
      await postsApi.create(form);
      qc.invalidateQueries({ queryKey: ['feed'] });
      onClose();
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl w-full max-w-md shadow-2xl">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="font-semibold text-gray-900">New Post</h2>
          <button onClick={onClose}><XMarkIcon className="w-5 h-5 text-gray-500" /></button>
        </div>

        <div className="p-4 space-y-4">
          {!preview
            ? <div {...getRootProps()} className={`border-2 border-dashed rounded-xl p-6 text-center cursor-pointer ${isDragActive ? 'border-pink-400 bg-pink-50' : 'border-gray-200 hover:border-gray-300'}`}>
                <input {...getInputProps()} />
                <PhotoIcon className="w-8 h-8 mx-auto text-gray-300 mb-1" />
                <p className="text-sm text-gray-400">Add a photo or video <span className="text-pink-500 font-medium">(optional)</span></p>
              </div>
            : <div className="relative rounded-xl overflow-hidden">
                {file?.type.startsWith('video/')
                  ? <video src={preview} className="w-full max-h-64 object-cover" controls />
                  : <img src={preview} alt="preview" className="w-full max-h-64 object-cover" />}
                <button onClick={() => { setFile(null); setPreview(null); }} className="absolute top-2 right-2 bg-black/50 text-white rounded-full p-1">
                  <XMarkIcon className="w-4 h-4" />
                </button>
              </div>
          }

          <textarea
            value={caption}
            onChange={e => setCaption(e.target.value)}
            placeholder={file ? 'Write a caption…' : 'What\'s on your mind?'}
            rows={3}
            className="w-full text-sm border border-gray-200 rounded-xl p-3 resize-none focus:outline-none focus:ring-2 focus:ring-pink-300"
          />
        </div>

        <div className="p-4 border-t flex justify-end">
          <button
            onClick={handleSubmit}
            disabled={!canSubmit}
            className="bg-pink-600 text-white text-sm font-semibold px-6 py-2 rounded-full disabled:opacity-40 hover:bg-pink-700"
          >
            {uploading ? 'Posting…' : 'Share'}
          </button>
        </div>
      </div>
    </div>
  );
}
