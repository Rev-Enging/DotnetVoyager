import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { UploadPage } from './pages/UploadPage/UploadPage';
import { AnalysisDashboard } from './pages/AnalysisDashboard/AnalysisDashboard';
import { About } from "./pages/About/About.tsx";
import './App.css';

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<UploadPage />} />
                <Route path="/analysis/:id" element={<AnalysisDashboard />} />
                <Route path="*" element={<Navigate to="/" replace />} />
                <Route path="/about" element={<About />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;