// Interface generated to mirror BlockFarmEditorLayoutDTO (C#)
// Guid fields are represented as strings (RFC 4122), dates as ISO strings

export interface Layout {
	id: number;
	key: string; // Guid
	name: string;
	description: string;
	layout: string;
	category: string;
    type: 'pageDefinition' | 'blockArea';
	icon: string;
	enabled: boolean;
	createDate?: string; // ISO datetime
	updateDate?: string; // ISO datetime
	deleteDate?: string | null; // ISO datetime or null
	createdBy?: string; // Guid
	updatedBy?: string; // Guid
}

export interface Layouts {
    [key: string]: Layout[];
}