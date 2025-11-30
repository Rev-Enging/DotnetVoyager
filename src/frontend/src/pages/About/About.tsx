import { MainLayout } from '../../components/MainLayout/MainLayout';
import { CubeIcon, GithubLogo, LinkedinLogo, CodeBlock, MagnifyingGlass, FileCode, Package } from '@phosphor-icons/react';
import './About.scss';

export const About = () => {
    return (
        <MainLayout showUploadNew={true}>
            <div className="about-page">
                <div className="about-container">
                    <header className="about-header">
                        <div className="logo-section">
                            <div className="big-logo">
                                <CubeIcon size={64} weight="fill" />
                            </div>
                            <h1>Dotnet<span className="highlight">Voyager</span></h1>
                            <p className="tagline">Advanced .NET Assembly Analysis & Decompilation Tool</p>
                        </div>
                    </header>

                    <section className="about-section">
                        <h2>About the Project</h2>
                        <p>
                            DotnetVoyager is a powerful web-based tool for analyzing, decompiling, and exploring .NET assemblies.
                            It provides developers with deep insights into compiled code, assembly structure, and dependencies.
                        </p>
                    </section>

                    <section className="features-section">
                        <h2>Key Features</h2>
                        <div className="features-grid">
                            <div className="feature-card">
                                <CodeBlock size={32} weight="duotone" className="feature-icon" />
                                <h3>Code Decompilation</h3>
                                <p>View decompiled C# code and IL instructions side by side with syntax highlighting</p>
                            </div>
                            <div className="feature-card">
                                <MagnifyingGlass size={32} weight="duotone" className="feature-icon" />
                                <h3>Assembly Explorer</h3>
                                <p>Navigate through namespaces, classes, methods, and properties with an intuitive tree view</p>
                            </div>
                            <div className="feature-card">
                                <FileCode size={32} weight="duotone" className="feature-icon" />
                                <h3>Metadata Analysis</h3>
                                <p>Examine assembly metadata, dependencies, and detailed statistics</p>
                            </div>
                            <div className="feature-card">
                                <Package size={32} weight="duotone" className="feature-icon" />
                                <h3>Export to ZIP</h3>
                                <p>Download complete decompiled source code as a ZIP archive for offline analysis</p>
                            </div>
                        </div>
                    </section>

                    <section className="about-section">
                        <h2>Technology Stack</h2>
                        <div className="tech-stack">
                            <div className="tech-column">
                                <h3>Frontend</h3>
                                <ul>
                                    <li>React + TypeScript</li>
                                    <li>React Router</li>
                                    <li>Axios</li>
                                    <li>SCSS</li>
                                    <li>Prism.js (syntax highlighting)</li>
                                </ul>
                            </div>
                            <div className="tech-column">
                                <h3>Backend</h3>
                                <ul>
                                    <li>.NET 8</li>
                                    <li>ASP.NET Core</li>
                                    <li>ICSharpCode.Decompiler</li>
                                    <li>System.Reflection.Metadata</li>
                                    <li>MediatR</li>
                                </ul>
                            </div>
                        </div>
                    </section>

                    <section className="about-section">
                        <h2>About the Developer</h2>
                        <p>
                            DotnetVoyager was created as a learning project to explore .NET metadata,
                            reflection, and decompilation technologies while building a modern web application.
                        </p>
                        <div className="contact-links">
                            <a href="https://github.com" target="_blank" rel="noopener noreferrer" className="contact-link">
                                <GithubLogo size={24} weight="fill" />
                                GitHub
                            </a>
                            {/*<a href="https://linkedin.com" target="_blank" rel="noopener noreferrer" className="contact-link">*/}
                            {/*    <LinkedinLogo size={24} weight="fill" />*/}
                            {/*    LinkedIn*/}
                            {/*</a>*/}
                        </div>
                    </section>

                    <footer className="about-footer">
                        <p>© 2025 DotnetVoyager. Built with ❤️ for the .NET community.</p>
                    </footer>
                </div>
            </div>
        </MainLayout>
    );
};