export class StorageVar<T> {
    constructor(
        public defValue: T,
        public name: string
    ) { }

    get value(): T {
        const val = localStorage.getItem(this.name);

        return <any>this.convertType(val);
    }

    set value(item: T | undefined) {
        if (!item) {
            localStorage.removeItem(this.name);
            return;
        }

        localStorage.setItem(this.name, <any>item);
    }

    convertType(value?: string | null) {
        if (value === undefined || value === null) {
            return this.defValue;
        }
        if (typeof this.defValue === 'number') {
            return Number(value);
        }
        if (typeof this.defValue === 'boolean') {
            return value === 'true';
        }

        return value;
    }
}