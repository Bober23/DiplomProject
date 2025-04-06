import React from 'react';
import { Routes, Route } from 'react-router-dom';
import DocumentListPage from './pages/DocumentListPage';
import ErrorBoundary from './components/ErrorBoundary';

const App = () => {
  return (
    <ErrorBoundary>
      <Routes>
        <Route 
          path="/" 
          element={<DocumentListPage />} 
          errorElement={<div>Error occurred!</div>}
        />
      </Routes>
    </ErrorBoundary>
  );
};

export default App;