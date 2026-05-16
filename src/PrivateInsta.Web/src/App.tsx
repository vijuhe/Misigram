import { useState } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './AuthContext';
import { ProtectedRoute } from './ProtectedRoute';
import { Layout } from './Layout';
import { LoginPage } from './pages/LoginPage';
import { FeedPage } from './pages/FeedPage';
import { StoriesPage } from './pages/StoriesPage';
import { PostDetailPage } from './pages/PostDetailPage';
import { ProfilePage } from './pages/ProfilePage';
import { EditProfilePage } from './pages/EditProfilePage';
import { MessagesPage } from './pages/MessagesPage';
import { ChatPage } from './pages/ChatPage';
import { NewPostModal } from './components/NewPostModal';
import { NewStoryModal } from './components/NewStoryModal';

function AppRoutes() {
  const [showPost, setShowPost] = useState(false);
  const [showStory, setShowStory] = useState(false);

  return (
    <>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/*" element={
          <ProtectedRoute>
            <Layout onNewPost={() => setShowPost(true)} onNewStory={() => setShowStory(true)}>
              <Routes>
                <Route path="/" element={<FeedPage />} />
                <Route path="/stories" element={<StoriesPage />} />
                <Route path="/post/:id" element={<PostDetailPage />} />
                <Route path="/profile/me" element={<EditProfilePage />} />
                <Route path="/profile/:id" element={<ProfilePage />} />
                <Route path="/messages" element={<MessagesPage />} />
                <Route path="/messages/:chatId" element={<ChatPage />} />
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </Layout>
          </ProtectedRoute>
        } />
      </Routes>

      {showPost && <NewPostModal onClose={() => setShowPost(false)} />}
      {showStory && <NewStoryModal onClose={() => setShowStory(false)} />}
    </>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <AppRoutes />
    </AuthProvider>
  );
}
