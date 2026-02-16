import type { BlockDefinition } from "../../../models/BlockDefinition";
import type { BlockDefinitions } from "../../../models/BlockDefinitions";
import type { IBuilderProperties } from "../../../models/IBuilderProperties";
import type { PageDefinition } from "../../../models/PageDefinition";


window.addEventListener(
    'message',
    (event: MessageEvent<{ messageType: string, data: PageDefinition, culture?: string }>) => {
        if (event.origin !== window.location.origin) return;
        if (event.data.messageType == 'blockfarmeditor:initialize') {
            fetch(window.blockFarmEditorBasePath + '/umbraco/blockfarmeditor/getblockdefinitions/',{
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                },
                method: 'POST',
            }).then((response) => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            }).then((blockDefinitions: BlockDefinitions) => {
                window.blockFarmEditorDefinitions = window.blockFarmEditorDefinitions || {};

                window.blockFarmEditorDefinitions = Object.entries(blockDefinitions).reduce((acc, [_, value]) => {
                    for (const block of value || []) {
                        acc[block.contentTypeKey] = block.name;
                    }
                    return acc;
                }, {} as Record<string, string>);

                for(const block of blockDefinitions?.blockDefinitions || []){
                    window.blockFarmEditorDefinitions[block.contentTypeKey] = block.name;
                }
                for(const block of blockDefinitions?.containerDefinitions || []){
                    window.blockFarmEditorDefinitions[block.contentTypeKey] = block.name;
                }
            }).catch((error) => {
                console.error('There was a problem with the fetch operation:', error);
            });

            let finalData = event.data.data;
            if (!finalData.blocks) {
                finalData.blocks = [];
            }
            if (!finalData.identifier) {
                finalData.identifier = 'BlockFarmEditor';
            }
            window.blockFarmEditor = {
                pageDefinition: event.data.data,
                culture: event.data.culture,
                /**
                 *
                 * @param uniquePath
                 * @returns
                 */
                getBlock: (uniquePath, create) => {
                    const pathParts = uniquePath.split('/').slice(1);
                    const unique = pathParts[pathParts.length - 1];
                    // Reference to the blocks array we need to traverse
                    let blocks: BlockDefinition<IBuilderProperties>[] = window.blockFarmEditor.pageDefinition.blocks || [];
                    // Navigate to the parent of the target block
                    for (let i = 0; i < pathParts.length; i++) {
                        const pathPart = pathParts[i];
                        // If this is the last part of the path, we want to return the block
                        // Otherwise, we want to navigate to the next level
                        if (pathParts.length - 1 == i) {
                            let block = blocks.find((block) => {
                                return block.unique == pathPart;
                            }) || null;
                            // If the block doesn't exist and create is true, create a new block
                            // Otherwise, return null
                            if (!block && create) {
                                blocks.push({ unique: pathPart, blocks: [] });
                                block = blocks.find((block) => {
                                    return block.unique == pathPart;
                                }) || null
                            }
                            return block;
                        }

                        let blockRef = blocks.find((block) => {
                            return block.unique == pathPart;
                        })

                        if (!blockRef) {
                            blocks.push({ unique: pathPart, blocks: [] });
                            blockRef = blocks.find((block) => {
                                return block.unique == pathPart;
                            })
                        }

                        blocks = blockRef?.blocks ?? [];
                    }
                    return window.blockFarmEditor.pageDefinition.blocks.find((block) => {
                        return block.unique == unique;
                    }) || null;
                },
                getBlockIndex: (uniquePath: string): number => {
                    const pathParts = uniquePath.split('/').slice(1);
                    if (pathParts.length === 0) return 0;
                    const unique = pathParts[pathParts.length - 1];
                    var parentBlock = window.blockFarmEditor.getBlock(pathParts.slice(0, -1).join('/'));
                    if (!parentBlock) return 0;
                    const currentIndex = parentBlock.blocks?.findIndex(block => block.unique === unique) ?? 0;
                    return currentIndex;
                },
                    
                    
                /**
                 * Updates a block at the specified path with new properties
                 * @param {string} uniquePath - Path to the block in format '|path|to|block'
                 * @param {BlockDefinition<IBuilderProperties>} updates - Object containing properties to update
                 * @returns {boolean} - Whether the update was successful
                 */
                updateBlock: (uniquePath: string, updates: BlockDefinition<IBuilderProperties>): boolean => {
                    if (!uniquePath) return false;

                    const pathParts = uniquePath.split('/').slice(1);
                    if (pathParts.length === 0) return false;

                    // Reference to the blocks array we need to traverse
                    let blocksRef = window.blockFarmEditor.pageDefinition.blocks || [];

                    // Navigate to the parent of the target block
                    for (let i = 0; i < pathParts.length - 1; i++) {
                        const pathPart = pathParts[i];
                        let blockIndex = blocksRef.findIndex(block => block.unique === pathPart);

                        if (blockIndex === -1) {
                            return false;
                        }
                        blockIndex = blocksRef.findIndex(block => block.unique === pathPart);

                        blocksRef = blocksRef[blockIndex].blocks || [];
                    }

                    // Find the target block in its parent's blocks array
                    const targetIdentifier = pathParts[pathParts.length - 1];
                    const targetIndex = blocksRef.findIndex(block => block.unique === targetIdentifier);

                    if (targetIndex === -1) {
                        blocksRef.push(updates)
                    } else {
                        // Update the block with new properties
                        blocksRef[targetIndex] = updates;
                    }

                    // Trigger an event to notify subscribers about the update
                    window.dispatchEvent(new CustomEvent('blockfarmeditor:blockUpdated', {
                        detail: {
                            path: uniquePath,
                            block: updates
                        }
                    }));

                    return true;
                },
                displayMessage: (color: 'info' | 'success' | 'warning' | 'danger', message: string, headline?: string) => {
                    window.parent.postMessage({
                        messageType: 'blockfarmeditor:message',
                        color: color,
                        headline: headline,
                        message: message
                    }, window.location.origin);
                }
            };
            window.dispatchEvent(new Event('blockfarmeditor:initialized'));
        }
        else if (event.data.messageType == 'blockfarmeditor:page-updated') {
            window.parent.postMessage({
                messageType: 'blockfarmeditor:page-updated',
                pageDefinition: window.blockFarmEditor.pageDefinition,
            });
        }
    },
    false,
);

window.addEventListener("load", () => {
    document.body.appendChild(document.createElement("block-layers"));
})

document.documentElement.addEventListener('mouseup', () => { 
    if(!window.blockFarmEditorDrag) return;
    window.blockFarmEditorDrag = null;
    const blockElements = document.body.querySelectorAll('.bfe--block-area-block');
    blockElements?.forEach((el) => {
        el.classList.remove('bfe--dragging','bfe--drag-over-top','bfe--drag-over-bottom','bfe--show-hover-top','bfe--show-hover-bottom');
    });
});

(function() {
    let scrollInterval: number | null = null;
    const scrollDistance = 10; // Adjust as needed
    const edgeThreshold = 100;  // Adjust as needed

    function startScrollingDown() {
        if (scrollInterval) return; // Prevent multiple intervals
        scrollInterval = setInterval(() => {
            window.scrollBy(0, scrollDistance);
        }, 10); // Adjust interval as needed
    }

    function startScrollingUp() {
        if (scrollInterval) return; // Prevent multiple intervals
        scrollInterval = setInterval(() => {
            window.scrollBy(0, -scrollDistance);
        }, 10); // Adjust interval as needed
    }

    function stopScrolling() {
        if (scrollInterval) {
            clearInterval(scrollInterval);
            scrollInterval = null;
        }
    }

    function handleDrag(event: Event) {
        const ce = event as CustomEvent;
        const viewportHeight = window.innerHeight;
        const elementY = ce.detail.clientY;

        // Check if near bottom edge for scrolling down
        if (viewportHeight - elementY < edgeThreshold) {
            stopScrolling(); // Stop any existing scroll
            startScrollingDown();
        }
        // Check if near top edge for scrolling up
        else if (elementY < edgeThreshold) {
            stopScrolling(); // Stop any existing scroll
            startScrollingUp();
        }
        // Not near any edge, stop scrolling
        else {
            stopScrolling();
        }
    }

    document.documentElement.addEventListener('block-drag-move', handleDrag);

    document.documentElement.addEventListener('block-drag-end', stopScrolling);

    document.documentElement.addEventListener('mouseup', () => {
        document.documentElement.dispatchEvent(new Event('block-drag-end'));
    });

    document.documentElement.addEventListener('mouseleave', () => {
        document.documentElement.dispatchEvent(new Event('block-drag-end'));
    });
})();