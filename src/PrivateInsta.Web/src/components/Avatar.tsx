import type { User } from '../types';

export function Avatar({ user, size = 'md', ring = false }: { user: User; size?: 'sm' | 'md' | 'lg'; ring?: boolean }) {
  const sizeClass = { sm: 'w-8 h-8 text-xs', md: 'w-10 h-10 text-sm', lg: 'w-16 h-16 text-xl' }[size];
  const initials = user.displayName.split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase();

  return (
    <div className={`relative inline-flex shrink-0 ${sizeClass} rounded-full ${ring ? 'ring-2 ring-pink-500 ring-offset-1' : ''}`}>
      {user.avatarUrl
        ? <img src={user.avatarUrl} alt={user.displayName} className={`${sizeClass} rounded-full object-cover`} />
        : <div className={`${sizeClass} rounded-full bg-gradient-to-br from-pink-400 to-purple-500 flex items-center justify-center text-white font-semibold`}>{initials}</div>
      }
    </div>
  );
}
