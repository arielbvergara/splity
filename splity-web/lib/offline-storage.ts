// IndexedDB wrapper for offline data storage

const DB_NAME = "splity-offline"
const DB_VERSION = 1

interface PendingAction {
  id: string
  type: "create_expense" | "update_expense" | "delete_expense" | "create_party"
  data: any
  timestamp: number
}

class OfflineStorage {
  private db: IDBDatabase | null = null

  async init(): Promise<void> {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(DB_NAME, DB_VERSION)

      request.onerror = () => reject(request.error)
      request.onsuccess = () => {
        this.db = request.result
        resolve()
      }

      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result

        // Create object stores
        if (!db.objectStoreNames.contains("pendingActions")) {
          db.createObjectStore("pendingActions", { keyPath: "id" })
        }

        if (!db.objectStoreNames.contains("cachedData")) {
          db.createObjectStore("cachedData", { keyPath: "key" })
        }
      }
    })
  }

  async addPendingAction(action: Omit<PendingAction, "id" | "timestamp">): Promise<void> {
    if (!this.db) await this.init()

    const pendingAction: PendingAction = {
      ...action,
      id: crypto.randomUUID(),
      timestamp: Date.now(),
    }

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction(["pendingActions"], "readwrite")
      const store = transaction.objectStore("pendingActions")
      const request = store.add(pendingAction)

      request.onsuccess = () => resolve()
      request.onerror = () => reject(request.error)
    })
  }

  async getPendingActions(): Promise<PendingAction[]> {
    if (!this.db) await this.init()

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction(["pendingActions"], "readonly")
      const store = transaction.objectStore("pendingActions")
      const request = store.getAll()

      request.onsuccess = () => resolve(request.result)
      request.onerror = () => reject(request.error)
    })
  }

  async removePendingAction(id: string): Promise<void> {
    if (!this.db) await this.init()

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction(["pendingActions"], "readwrite")
      const store = transaction.objectStore("pendingActions")
      const request = store.delete(id)

      request.onsuccess = () => resolve()
      request.onerror = () => reject(request.error)
    })
  }

  async cacheData(key: string, data: any): Promise<void> {
    if (!this.db) await this.init()

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction(["cachedData"], "readwrite")
      const store = transaction.objectStore("cachedData")
      const request = store.put({ key, data, timestamp: Date.now() })

      request.onsuccess = () => resolve()
      request.onerror = () => reject(request.error)
    })
  }

  async getCachedData(key: string): Promise<any> {
    if (!this.db) await this.init()

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction(["cachedData"], "readonly")
      const store = transaction.objectStore("cachedData")
      const request = store.get(key)

      request.onsuccess = () => resolve(request.result?.data)
      request.onerror = () => reject(request.error)
    })
  }
}

export const offlineStorage = new OfflineStorage()
