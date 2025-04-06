import React, { createContext, useState, useContext, useEffect } from 'react';
import { message } from 'antd';
import { useNavigate } from 'react-router-dom';
import CryptoJS from 'crypto-js';

const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      setUser(JSON.parse(storedUser));
    }
    setLoading(false);
  }, []);

  const login = async (email, password) => {
    try {
      const response = await fetch('http://localhost:5120/api/User/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email,
          passwordHash: CryptoJS.SHA256(password).toString(),
        }),
      });
  
      const userData = await response.json(); // Всегда парсим JSON
  
      if (!response.ok) {
        // Используем сообщение от сервера или стандартное
        throw new Error(userData.message || `Ошибка ${response.status}`);
      }
  
      setUser({
        id: userData.id,
        email: userData.email,
      });
      localStorage.setItem('user', JSON.stringify(userData));
      message.success('Вход выполнен успешно');
      navigate('/');
      return true; // Успешный вход
    } catch (error) {
      console.error('Login error:', error);
      // Формируем понятное сообщение об ошибке
      let errorMessage = 'Ошибка при входе';
      
      console.log(error.message);
      if (error.message.includes('400')) {
        errorMessage = 'Неверный формат данных';
      } else if (error.message.includes('401')) {
        errorMessage = 'Неверный email или пароль';
      } else if (error.message.includes('Network Error')) {
        errorMessage = 'Сервер недоступен';
      }
      
      message.error(errorMessage);
      throw new Error(errorMessage); // Пробрасываем для компонента
    }
  };
  const logout = () => {
    setUser(null);
    localStorage.removeItem('user');
    navigate('/login');
  };

  if (loading) {
    return <div>Загрузка...</div>;
  }

  return (
    <AuthContext.Provider value={{ user, login, logout, loading }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};