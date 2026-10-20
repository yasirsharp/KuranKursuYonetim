import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import LoginPage from './pages/auth/LoginPage';
import OtpVerifyPage from './pages/auth/OtpVerifyPage';
import CourseApplicationPage from './pages/auth/CourseApplicationPage';
import ApplicationStatusPage from './pages/auth/ApplicationStatusPage';
import TeacherLayout from './layouts/TeacherLayout';
import PlatformLayout from './layouts/PlatformLayout';
import DashboardPage from './pages/teacher/DashboardPage';
import StudentListPage from './pages/teacher/StudentListPage';
import StudentDetailPage from './pages/teacher/StudentDetailPage';
import LessonFormPage from './pages/teacher/LessonFormPage';
import StudentFormPage from './pages/teacher/StudentFormPage';
import SettingsPage from './pages/teacher/SettingsPage';
import CalendarPage from './pages/teacher/CalendarPage';
import MorePage from './pages/teacher/MorePage';
import PlatformLoginPage from './pages/platform/PlatformLoginPage';
import PlatformTenantsPage from './pages/platform/PlatformTenantsPage';

import AttendancePage from './pages/teacher/AttendancePage';
import EtutPage from './pages/teacher/EtutPage';

import { isApplicationOnly, isPlatformUser } from './services/session';
import { homePathFor, PATHS } from './utils/navigation';

import './styles/globals.css';
import './styles/layouts.css';
import './styles/forms.css';

/** Kurs uygulaması — giriş gerekir; onay bekleyen kurs ve platform yöneticisi kendi ekranlarına gider */
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  if (!user) return <Navigate to={PATHS.login} replace />;
  if (isPlatformUser(user) || isApplicationOnly(user)) return <Navigate to={homePathFor(user)} replace />;
  return <>{children}</>;
}

/** Başvuru durumu ekranı — yalnızca kurs kullanıcıları */
function ApplicationRoute({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  if (!user) return <Navigate to={PATHS.login} replace />;
  if (isPlatformUser(user)) return <Navigate to={PATHS.platform} replace />;
  return <>{children}</>;
}

/** Platform paneli — yalnızca platform yöneticisi */
function PlatformRoute({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  if (!user || !isPlatformUser(user)) return <Navigate to={PATHS.platformLogin} replace />;
  return <>{children}</>;
}

/** Giriş yapmışsa giriş/başvuru sayfalarını göstermeden kendi ana sayfasına gider */
function PublicRoute({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  if (user) return <Navigate to={homePathFor(user)} replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Auth sayfaları */}
          <Route path={PATHS.login} element={<PublicRoute><LoginPage /></PublicRoute>} />
          <Route path={PATHS.verifyOtp} element={<PublicRoute><OtpVerifyPage /></PublicRoute>} />
          <Route path={PATHS.courseApplication} element={<PublicRoute><CourseApplicationPage /></PublicRoute>} />
          <Route path={PATHS.applicationStatus} element={<ApplicationRoute><ApplicationStatusPage /></ApplicationRoute>} />

          {/* Platform yöneticisi */}
          <Route path={PATHS.platformLogin} element={<PublicRoute><PlatformLoginPage /></PublicRoute>} />
          <Route
            path={PATHS.platform}
            element={
              <PlatformRoute>
                <PlatformLayout />
              </PlatformRoute>
            }
          >
            <Route index element={<PlatformTenantsPage />} />
          </Route>

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
            <Route path="settings" element={<SettingsPage />} />
            <Route path="calendar" element={<CalendarPage />} />
            <Route path="more" element={<MorePage />} />
          </Route>

          {/* 404 → Ana sayfaya yönlendir */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
