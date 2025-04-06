import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './components/AuthContext';
import Header from './components/Header';
import LoginPage from './pages/LoginPage';
import RegistrationPage from './pages/RegistrationPage';
import DocumentListPage from './pages/DocumentListPage';

const PrivateRoute = ({ children }) => {
  const { user, loading } = useAuth();

  if (loading) {
    return <div>Загрузка...</div>; // Лоадер во время проверки
  }

  return user ? children : <Navigate to="/login" replace />;
};

const App = () => {
  return (
    <AuthProvider>
      <Header />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegistrationPage />} />
        <Route
          path="/"
          element={
            <PrivateRoute>
              <DocumentListPage />
            </PrivateRoute>
          }
        />
      </Routes>
    </AuthProvider>
  );
};

export default App;