import { createContext, useContext, useEffect, useState } from 'react';
import { auth } from './api';
import type { User } from './types';

interface AuthState {
  user: User | null;
  loading: boolean;
  refetch: () => void;
}

const AuthContext = createContext<AuthState>({ user: null, loading: true, refetch: () => {} });

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    auth.me()
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  return <AuthContext.Provider value={{ user, loading, refetch: load }}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);
