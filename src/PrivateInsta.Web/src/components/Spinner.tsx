export function Spinner({ className = '' }: { className?: string }) {
  return (
    <div className={`flex justify-center items-center py-16 ${className}`}>
      <div className="w-8 h-8 border-2 border-pink-200 border-t-pink-600 rounded-full animate-spin" />
    </div>
  );
}
