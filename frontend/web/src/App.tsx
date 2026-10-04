import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import LoginPage from './pages/auth/LoginPage';
import OtpVerifyPage from './pages/auth/OtpVerifyPage';
import TeacherLayout from './layouts/TeacherLayout';
import DashboardPage from './pages/teacher/DashboardPage';
import StudentListPage from './pages/teacher/StudentListPage';
import StudentDetailPage from './pages/teacher/StudentDetailPage';
import LessonFormPage from './pages/teacher/LessonFormPage';
import StudentFormPage from './pages/teacher/StudentFormPage';

import AttendancePage from './pages/teacher/AttendancePage';
import EtutPage from './pages/teacher/EtutPage';

import './styles/globals.css';
import './styles/layouts.css';

/** Korumalı rotalar — login olmadan erişilemez */
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
}

/** Giriş yapmışsa login'e gitmesin */
function PublicRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }
  return <>{children}</>;
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Auth sayfaları */}
          <Route path="/login" element={<PublicRoute><LoginPage /></PublicRoute>} />
          <Route path="/verify-otp" element={<PublicRoute><OtpVerifyPage /></PublicRoute>} />

          {/* Teacher Layout Sayfaları */}
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <TeacherLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<DashboardPage />} />
            <Route path="students" element={<StudentListPage />} />
            <Route path="students/new" element={<StudentFormPage />} />
            <Route path="students/:id" element={<StudentDetailPage />} />
            <Route path="students/:id/edit" element={<StudentFormPage />} />
            <Route path="lessons/new" element={<LessonFormPage />} />
            <Route path="attendance" element={<AttendancePage />} />
            <Route path="etut" element={<EtutPage />} />
            <Route path="more" element={<div style={{padding: 20}}>Daha Fazla Menüsü (Yapım Aşamasında)</div>} />
          </Route>

          {/* 404 → Ana sayfaya yönlendir */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
