import { Link, useLocation, useNavigate } from 'react-router-dom';
import { auth } from './api';
import { useAuth } from './AuthContext';
import {
  HomeIcon,
  ChatBubbleLeftRightIcon,
  UserCircleIcon,
  PlusCircleIcon,
  BookOpenIcon,
} from '@heroicons/react/24/outline';

export function Layout({ children, onNewPost, onNewStory }: {
  children: React.ReactNode;
  onNewPost?: () => void;
  onNewStory?: () => void;
}) {
  const { user, refetch } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await auth.logout();
    refetch();
    navigate('/login');
  };

  const nav = [
    { to: '/', icon: HomeIcon, label: 'Feed' },
    { to: '/stories', icon: BookOpenIcon, label: 'Stories' },
    { to: '/messages', icon: ChatBubbleLeftRightIcon, label: 'Messages' },
    { to: `/profile/${user?.id}`, icon: UserCircleIcon, label: 'Profile' },
  ];

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Top nav */}
      <header className="fixed top-0 left-0 right-0 z-40 bg-white border-b border-gray-200">
        <div className="max-w-screen-md mx-auto flex items-center justify-between px-4 h-14">
          <Link to="/" className="text-xl font-bold text-pink-600 tracking-tight">Misigram</Link>
          <div className="flex items-center gap-3">
            <button onClick={onNewStory} className="text-xs text-gray-500 hover:text-pink-600 font-medium px-2 py-1 rounded-lg hover:bg-pink-50">+ Story</button>
            <button onClick={onNewPost} className="flex items-center gap-1 bg-pink-600 text-white text-xs font-semibold px-3 py-1.5 rounded-full hover:bg-pink-700">
              <PlusCircleIcon className="w-4 h-4" /> Post
            </button>
            <button onClick={handleLogout} className="text-xs text-gray-400 hover:text-gray-700">Sign out</button>
          </div>
        </div>
      </header>

      {/* Page content */}
      <main className="pt-14 pb-16 max-w-screen-md mx-auto px-4">
        {children}
      </main>

      {/* Bottom tab bar */}
      <nav className="fixed bottom-0 left-0 right-0 z-40 bg-white border-t border-gray-200">
        <div className="max-w-screen-md mx-auto flex justify-around">
          {nav.map(({ to, icon: Icon, label }) => {
            const active = location.pathname === to;
            return (
              <Link key={to} to={to} className={`flex flex-col items-center py-2 px-4 ${active ? 'text-pink-600' : 'text-gray-400'}`}>
                <Icon className="w-6 h-6" />
                <span className="text-xs mt-0.5">{label}</span>
              </Link>
            );
          })}
        </div>
      </nav>
    </div>
  );
}
