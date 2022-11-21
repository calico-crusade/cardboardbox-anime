export interface Definition {
    word: string;
    phonetic: string;
    phonetics: {
        text: string;
        audio?: string;
        sourceUrl?: string;
        license?: {
            name: string;
            url: string;
        };
    }[];
    origin: string;
    meanings: {
        partOfSpeech: string;
        definitions: {
            definition: string;
            example?: string;
            synonyms: string[];
            antonyms: string[];
        }[];
        synonyms: string[];
        antonyms: string[];
    }[];
    license?: {
        name: string;
        url: string;
    };
    sourceUrls: string[];
}