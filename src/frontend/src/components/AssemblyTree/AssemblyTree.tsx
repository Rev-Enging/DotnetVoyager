import React, { useState, useEffect, useMemo } from 'react';
import classNames from 'classnames';
import { CaretRightIcon, CaretDownIcon, DatabaseIcon, FolderIcon, FileCodeIcon, CubeIcon, BracketsCurlyIcon, FunctionIcon } from '@phosphor-icons/react';
import type { AssemblyTreeNodeDto, AssemblyTreeNodeType } from '../../models/analysis';
import './AssemblyTree.scss';

interface AssemblyTreeProps {
    node: AssemblyTreeNodeDto;
    onSelectNode: (token: number, type: AssemblyTreeNodeType) => void;
    selectedToken?: number;
    depth?: number;
    searchQuery?: string; // Додано проп для пошуку
}

export const AssemblyTree = ({
    node,
    onSelectNode,
    selectedToken,
    depth = 0,
    searchQuery = ''
}: AssemblyTreeProps) => {

    // Перевіряємо, чи підходить поточна нода під пошук
    const matchesSearch = useMemo(() => {
        if (!searchQuery) return true;
        return node.name.toLowerCase().includes(searchQuery.toLowerCase());
    }, [node.name, searchQuery]);

    // Перевіряємо дітей. Це трохи "важка" операція для великих дерев,
    // але для UI дерева збірки (зазвичай < 10k нод) це ок.
    // Ми фільтруємо дітей, щоб знати, чи є сенс показувати цю ноду.
    const filteredChildren = useMemo(() => {
        if (!node.children) return [];
        if (!searchQuery) return node.children;

        // Допоміжна функція для "глибокої" перевірки, чи є збіг у гілці
        const hasMatchDeep = (n: AssemblyTreeNodeDto): boolean => {
            const selfMatch = n.name.toLowerCase().includes(searchQuery.toLowerCase());
            if (selfMatch) return true;
            return n.children?.some(child => hasMatchDeep(child)) ?? false;
        };

        return node.children.filter(child => hasMatchDeep(child));
    }, [node.children, searchQuery]);

    const hasVisibleChildren = filteredChildren.length > 0;

    // Нода видима, якщо вона сама підходить, або в неї є діти, що підходять
    const isVisible = !searchQuery || matchesSearch || hasVisibleChildren;

    // Стан розгортання
    const [isExpanded, setIsExpanded] = useState(depth === 0);

    // Ефект: якщо йде пошук і є знайдені діти — розгортаємо автоматично
    useEffect(() => {
        if (searchQuery && hasVisibleChildren) {
            setIsExpanded(true);
        } else if (!searchQuery && depth !== 0) {
            // Опціонально: згорнути назад, коли пошук очищено (крім кореня)
            setIsExpanded(false);
        }
    }, [searchQuery, hasVisibleChildren, depth]);

    const handleToggle = (e: React.MouseEvent) => {
        e.stopPropagation();
        setIsExpanded(!isExpanded);
    };

    const handleSelect = (e: React.MouseEvent) => {
        e.stopPropagation();
        if (node.type === 'Method' || node.type === 'Class' || node.type === 'Interface') {
            onSelectNode(node.token, node.type);
        }
        // Також розгортаємо при кліку, якщо є діти
        if (hasVisibleChildren && !isExpanded) setIsExpanded(true);
    };

    const getIcon = (type: AssemblyTreeNodeType) => {
        switch (type) {
            case 'Assembly': return <DatabaseIcon weight="fill" className="icon-assembly" />;
            case 'Namespace': return <FolderIcon weight="fill" className="icon-namespace" />;
            case 'Class': return <FileCodeIcon weight="fill" className="icon-class" />;
            case 'Interface': return <BracketsCurlyIcon weight="bold" className="icon-interface" />;
            case 'Method': return <CubeIcon weight="fill" className="icon-method" />;
            default: return <FunctionIcon />;
        }
    };

    const isSelected = selectedToken === node.token;

    // Якщо нода не відповідає критеріям пошуку і не має відповідних дітей - не рендеримо нічого
    if (!isVisible) return null;

    return (
        <div className="assembly-tree-node">
            <div
                className={classNames('node-row', { 'node-selected': isSelected })}
                style={{ paddingLeft: `${depth * 12 + 8}px` }}
                onClick={handleSelect}
            >
                <span className="toggle-icon" onClick={handleToggle}>
                    {hasVisibleChildren && (
                        isExpanded ? <CaretDownIcon size={12} /> : <CaretRightIcon size={12} />
                    )}
                    {!hasVisibleChildren && <span style={{ width: 12 }} />}
                </span>

                <span className="type-icon">{getIcon(node.type)}</span>

                <span className="node-name">
                    {/* Підсвітка тексту при пошуку (опціонально, проста реалізація) */}
                    {searchQuery && matchesSearch ? (
                        <span className="highlight-text">{node.name}</span>
                    ) : (
                        node.name
                    )}
                </span>
            </div>

            {hasVisibleChildren && isExpanded && (
                <div className="node-children">
                    {filteredChildren.map((child) => (
                        <AssemblyTree
                            key={`${child.token}-${child.name}`}
                            node={child}
                            onSelectNode={onSelectNode}
                            selectedToken={selectedToken}
                            depth={depth + 1}
                            searchQuery={searchQuery} // Прокидуємо проп вниз
                        />
                    ))}
                </div>
            )}
        </div>
    );
};