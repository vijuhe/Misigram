import { useEffect, useRef, useState } from 'react';
import { XMarkIcon, ChevronLeftIcon, ChevronRightIcon } from '@heroicons/react/24/solid';
import type { Story } from '../types';

interface Props {
  stories: Story[];
  startIndex?: number;
  onClose: () => void;
}

export function StoryViewer({ stories, startIndex = 0, onClose }: Props) {
  const [index, setIndex] = useState(startIndex);
  const [progress, setProgress] = useState(0);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const DURATION = 5000;

  const startProgress = () => {
    setProgress(0);
    intervalRef.current && clearInterval(intervalRef.current);
    const start = Date.now();
    intervalRef.current = setInterval(() => {
      const elapsed = Date.now() - start;
      const pct = Math.min((elapsed / DURATION) * 100, 100);
      setProgress(pct);
      if (pct >= 100) next();
    }, 50);
  };

  const next = () => {
    setIndex(i => {
      if (i + 1 >= stories.length) { onClose(); return i; }
      return i + 1;
    });
  };

  const prev = () => setIndex(i => Math.max(0, i - 1));

  useEffect(() => {
    startProgress();
    return () => { intervalRef.current && clearInterval(intervalRef.current); };
  }, [index]);

  const story = stories[index];
  if (!story) return null;

  return (
    <div className="fixed inset-0 z-50 bg-black flex items-center justify-center" onClick={onClose}>
      <div className="relative max-w-sm w-full h-full md:h-[90vh] md:rounded-xl overflow-hidden" onClick={e => e.stopPropagation()}>
        {/* Progress bars */}
        <div className="absolute top-3 left-3 right-3 flex gap-1 z-10">
          {stories.map((_, i) => (
            <div key={i} className="flex-1 h-0.5 bg-white/40 rounded">
              <div className="h-full bg-white rounded transition-none" style={{ width: i < index ? '100%' : i === index ? `${progress}%` : '0%' }} />
            </div>
          ))}
        </div>

        {/* Author */}
        <div className="absolute top-8 left-3 z-10 flex items-center gap-2">
          <div className="w-8 h-8 rounded-full bg-white/30 overflow-hidden">
            {story.author.avatarUrl && <img src={story.author.avatarUrl} alt="" className="w-full h-full object-cover" />}
          </div>
          <span className="text-white text-sm font-medium">{story.author.displayName}</span>
        </div>

        {/* Close */}
        <button onClick={onClose} className="absolute top-8 right-3 z-10 text-white">
          <XMarkIcon className="w-6 h-6" />
        </button>

        {/* Media */}
        {story.mediaType === 'Video'
          ? <video src={story.mediaUrl} className="w-full h-full object-cover" autoPlay muted playsInline />
          : <img src={story.mediaUrl} alt="" className="w-full h-full object-cover" />}

        {/* Navigation */}
        <button onClick={prev} className="absolute left-0 top-0 bottom-0 w-1/3" aria-label="Previous" />
        <button onClick={next} className="absolute right-0 top-0 bottom-0 w-1/3" aria-label="Next" />
        {index > 0 && <ChevronLeftIcon className="absolute left-2 top-1/2 -translate-y-1/2 w-8 h-8 text-white pointer-events-none" />}
        {index < stories.length - 1 && <ChevronRightIcon className="absolute right-2 top-1/2 -translate-y-1/2 w-8 h-8 text-white pointer-events-none" />}
      </div>
    </div>
  );
}
