import classNames from 'classnames';
import { CodeIcon, GearIcon, ColumnsIcon } from '@phosphor-icons/react';
import './CodeViewer.scss';
import Prism from 'prismjs';
import '../../styles/prism-vs-dark.css';
import 'prismjs/components/prism-csharp';
import 'prismjs/components/prism-clike';
import 'prismjs/components/prism-asm6502';
import { useEffect, useRef, useState } from 'react';

type ViewMode = 'csharp' | 'il' | 'both';

interface CodeViewerProps {
    csharpCode?: string;
    ilCode?: string;
    viewMode: ViewMode;
    onChangeViewMode: (mode: ViewMode) => void;
    isLoading: boolean;
}

const INITIAL_SPLIT = 50;

export const CodeViewer = ({ csharpCode, ilCode, viewMode, onChangeViewMode, isLoading }: CodeViewerProps) => {

    const csRef = useRef<HTMLElement>(null);
    const ilRef = useRef<HTMLElement>(null);
    const contentAreaRef = useRef<HTMLDivElement>(null);
    const [csharpWidth, setCsharpWidth] = useState(INITIAL_SPLIT);

    // Стан для відстеження перетягування
    const [isResizing, setIsResizing] = useState(false);

    useEffect(() => {
        Prism.highlightAll();
    }, [csharpCode, ilCode, viewMode]);

    // Обробник початку перетягування
    const handleMouseDown = (e: React.MouseEvent) => {
        if (viewMode === 'both') {
            e.preventDefault();
            setIsResizing(true);
        }
    };

    // Обробник руху миші
    const handleMouseMove = (e: MouseEvent) => {
        if (!isResizing || !contentAreaRef.current) return;

        const container = contentAreaRef.current;
        const containerRect = container.getBoundingClientRect();

        // Позиція миші відносно лівого краю контейнера
        const x = e.clientX - containerRect.left;

        // Обчислення нової ширини в процентах
        let newWidthPercent = (x / containerRect.width) * 100;

        newWidthPercent = Math.max(10, Math.min(90, newWidthPercent));

        setCsharpWidth(newWidthPercent);
    };

    // Обробник завершення перетягування
    const handleMouseUp = () => {
        setIsResizing(false);
    };

    useEffect(() => {
        if (isResizing) {
            document.addEventListener('mousemove', handleMouseMove);
            document.addEventListener('mouseup', handleMouseUp);
        } else {
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
        }

        // Прибираємо слухачі при демонтажі компонента
        return () => {
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
        };
    }, [isResizing]); // Залежність тільки від isResizing

    return (
        <div className="code-viewer">
            <div className="tabs-bar">
                <button
                    className={classNames('tab-btn', { active: viewMode === 'csharp' })}
                    onClick={() => onChangeViewMode('csharp')}
                >
                    <CodeIcon className="icon-cs" /> Csharp Code
                </button>
                <button
                    className={classNames('tab-btn', { active: viewMode === 'il' })}
                    onClick={() => onChangeViewMode('il')}
                >
                    <GearIcon className="icon-il" /> IL Code
                </button>
                <button
                    className={classNames('tab-btn split-btn', { active: viewMode === 'both' })}
                    onClick={() => onChangeViewMode('both')}
                >
                    <ColumnsIcon /> Split View
                </button>
            </div>

            <div
                className={classNames("code-content-area", { 'resizing': isResizing })}
                ref={contentAreaRef}
            >
                {isLoading ? (
                    <div className="loading-state">Loading Code...</div>
                ) : (
                    <>
                        {(viewMode === 'csharp' || viewMode === 'both') && (
                            <div
                                className="panel csharp-panel"
                                style={viewMode === 'both' ? { width: `${csharpWidth}%` } : undefined}
                            >
                                <div className="lang-label">// Decompiled C# Code</div>

                                <pre className="code-block">
                                    <code
                                        ref={csRef}
                                        className="language-csharp"
                                    >
                                        {csharpCode || '// Select a method to view code'}
                                    </code>
                                </pre>
                            </div>
                        )}

                        {viewMode === 'both' && (
                            <div
                                className="resizer"
                                onMouseDown={handleMouseDown}
                            />
                        )}

                        {(viewMode === 'il' || viewMode === 'both') && (
                            <div
                                className="panel il-panel"
                                style={viewMode === 'both' ? { width: `${100 - csharpWidth}%` } : undefined}
                            >
                                <div className="lang-label">// IL Instructions</div>

                                <pre className="code-block">
                                    <code
                                        ref={ilRef}
                                        className="language-asm6502"
                                    >
                                        {ilCode || ''}
                                    </code>
                                </pre>
                            </div>
                        )}
                    </>
                )}
            </div>
        </div>
    );
};