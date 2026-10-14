import { createRoot } from 'react-dom/client';
import App from './App';

const rootElement = document.getElementById('root');
if (!rootElement) throw new Error('Root element not found');

// NOT: StrictMode kaldırıldı — Development'ta useEffect'i 2x tetikleyip
// eşzamanlı API çağrılarına ve backend DbContext hatasına yol açıyordu.
createRoot(rootElement).render(<App />);
