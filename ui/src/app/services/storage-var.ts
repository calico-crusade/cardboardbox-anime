import { Capacitor } from '@capacitor/core';
import { Preferences } from '@capacitor/preferences';

const DEFAULT_PLATFORM = 'web';

export function checkPlatform() {
    try {
        return Capacitor?.getPlatform() || DEFAULT_PLATFORM;
    } catch(err) {
        console.error('Failure to fetch platform', { err });
        return DEFAULT_PLATFORM;
    }
}

export interface IStorageVar<T> {
    get value(): T;
    set value(item: T | undefined);
}

export class StorageVar<T> implements IStorageVar<T> {
    static STORAGE_TARGET: ('local' | 'capacitor') = 'capacitor';

    private _local: LocalStorageVar<T>;
    private _capac: CapacitorStorageVar<T>;

    constructor(
        public defValue: T,
        public name: string,
        public fun?: (val?: T) => void
    ) { 
        this._local = new LocalStorageVar<T>(defValue, name, fun);
        this._capac = new CapacitorStorageVar<T>(defValue, name, fun);
    }

    get value(): T {
        try {
            if (StorageVar.STORAGE_TARGET === 'capacitor')  {
                return this._capac.value;
            }

            return this._local.value;
        } catch (err) {
            console.error('Error fetching storage variable', { name: this.name, err });
            if (StorageVar.STORAGE_TARGET === 'local') return this.defValue;

            StorageVar.STORAGE_TARGET = 'local';
            return this._local.value;
        }
    }

    set value(item: T | undefined) {
        try {
            if (StorageVar.STORAGE_TARGET === 'capacitor')  {
                this._capac.value = item;
            }

            this._local.value = item;
        } catch (err) {
            console.error('Error fetching storage variable', { name: this.name, err });
            if (StorageVar.STORAGE_TARGET === 'local') return;

            StorageVar.STORAGE_TARGET = 'local';
            this._local.value = item;
        }
    }
}

export class LocalStorageVar<T> implements IStorageVar<T> {
    private _value?: T;

    constructor(
        public defValue: T,
        public name: string,
        public fun?: (val?: T) => void
    ) { }

    get value(): T { 
        if (this._value !== undefined) return this._value;
        const val = localStorage.getItem(this.name);
        return this._value = <any>this.convertType(val); 
    }
    set value(item: T | undefined) { 
        this._value = item;
        if (item !== undefined) localStorage.setItem(this.name, <any>item);
        else localStorage.removeItem(this.name);
        if (this.fun) this.fun(item);
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

export type Dic = { [key: string]: any };

export class CapacitorStorageVar<T> implements IStorageVar<T> {
    private static _values?: Dic;

    constructor(
        public defValue: T,
        public name: string,
        public fun?: (val?: T) => void
    ) { }

    get value(): T {
        if (!CapacitorStorageVar._values) return this.defValue;

        const item = CapacitorStorageVar._values[this.name];
        if (!item) return this.defValue;

        return item;
    }

    set value(item: T | undefined) {
        if (CapacitorStorageVar._values === undefined) CapacitorStorageVar._values = {};

        CapacitorStorageVar._values[this.name] = item;
        this.saveVars();
        if (this.fun) this.fun(item);
    }

    static async init() {
        try {
            const { value } = await Preferences.get({ key: 'local-storage' });

            if (!value) {
                CapacitorStorageVar._values = {};
                return;
            }

            CapacitorStorageVar._values = JSON.parse(value);
            console.log('Capacitor Storage loaded with', { value: CapacitorStorageVar._values });
        } catch (err) {
            console.error('Error occurred fetching capacitor local storage', { err });
            StorageVar.STORAGE_TARGET = 'local';
        }
    }

    async saveVars() {
        await Preferences.set({ key: 'local-storage', value: JSON.stringify(CapacitorStorageVar._values) });
    }
}