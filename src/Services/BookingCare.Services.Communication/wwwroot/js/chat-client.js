/**
 * SignalR Chat Client Example
 * Sử dụng Microsoft SignalR JavaScript Client
 * 
 * Installation:
 * npm install @microsoft/signalr
 */

class ChatClient {
    constructor(hubUrl, accessToken) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => accessToken
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.setupEventHandlers();
    }

    /**
     * Thiết lập các event handlers
     */
    setupEventHandlers() {
        // Nhận tin nhắn mới
        this.connection.on("ReceiveMessage", (message) => {
            console.log("Received message:", message);
            this.onMessageReceived(message);
        });

        // Tin nhắn đã được đọc
        this.connection.on("MessageRead", (data) => {
            console.log("Message read:", data);
            this.onMessageRead(data);
        });

        // Tất cả tin nhắn đã được đọc
        this.connection.on("AllMessagesRead", (data) => {
            console.log("All messages read:", data);
            this.onAllMessagesRead(data);
        });

        // User online/offline
        this.connection.on("UserOnline", (userId) => {
            console.log("User online:", userId);
            this.onUserOnline(userId);
        });

        this.connection.on("UserOffline", (userId) => {
            console.log("User offline:", userId);
            this.onUserOffline(userId);
        });

        // Typing indicators
        this.connection.on("UserStartedTyping", (data) => {
            console.log("User started typing:", data);
            this.onUserStartedTyping(data.UserId, data.ConversationId);
        });

        this.connection.on("UserStoppedTyping", (data) => {
            console.log("User stopped typing:", data);
            this.onUserStoppedTyping(data.UserId, data.ConversationId);
        });

        // Connection events
        this.connection.on("JoinedConversation", (conversationId) => {
            console.log("Joined conversation:", conversationId);
        });

        this.connection.on("LeftConversation", (conversationId) => {
            console.log("Left conversation:", conversationId);
        });

        this.connection.on("Error", (error) => {
            console.error("SignalR error:", error);
            this.onError(error);
        });

        // Conversation created
        this.connection.on("ConversationCreated", (conversation) => {
            console.log("New conversation created:", conversation);
            this.onConversationCreated(conversation);
        });

        // Online users list
        this.connection.on("OnlineUsers", (users) => {
            console.log("Online users:", users);
            this.onOnlineUsersReceived(users);
        });
    }

    /**
     * Kết nối đến SignalR Hub
     */
    async connect() {
        try {
            await this.connection.start();
            console.log("SignalR connected successfully");
            return true;
        } catch (error) {
            console.error("SignalR connection failed:", error);
            return false;
        }
    }

    /**
     * Ngắt kết nối
     */
    async disconnect() {
        try {
            await this.connection.stop();
            console.log("SignalR disconnected");
        } catch (error) {
            console.error("SignalR disconnection error:", error);
        }
    }

    /**
     * Tham gia conversation
     */
    async joinConversation(conversationId) {
        try {
            await this.connection.invoke("JoinConversation", conversationId);
        } catch (error) {
            console.error("Error joining conversation:", error);
        }
    }

    /**
     * Rời khỏi conversation
     */
    async leaveConversation(conversationId) {
        try {
            await this.connection.invoke("LeaveConversation", conversationId);
        } catch (error) {
            console.error("Error leaving conversation:", error);
        }
    }



    /**
     * Đánh dấu tin nhắn đã đọc
     */
    async markMessageAsRead(messageId) {
        try {
            await this.connection.invoke("MarkMessageAsRead", messageId);
        } catch (error) {
            console.error("Error marking message as read:", error);
        }
    }

    /**
     * Đánh dấu tất cả tin nhắn đã đọc
     */
    async markAllMessagesAsRead(conversationId) {
        try {
            await this.connection.invoke("MarkAllMessagesAsRead", conversationId);
        } catch (error) {
            console.error("Error marking all messages as read:", error);
        }
    }

    /**
     * Bắt đầu gõ
     */
    async startTyping(conversationId) {
        try {
            await this.connection.invoke("StartTyping", conversationId);
        } catch (error) {
            console.error("Error starting typing:", error);
        }
    }

    /**
     * Dừng gõ
     */
    async stopTyping(conversationId) {
        try {
            await this.connection.invoke("StopTyping", conversationId);
        } catch (error) {
            console.error("Error stopping typing:", error);
        }
    }

    /**
     * Lấy danh sách users online
     */
    async getOnlineUsers() {
        try {
            await this.connection.invoke("GetOnlineUsers");
        } catch (error) {
            console.error("Error getting online users:", error);
        }
    }

    // Event handlers - override these in your implementation
    onMessageReceived(message) {
        // Implement UI update for new message
    }

    onMessageRead(data) {
        // Implement UI update for message read status
    }

    onAllMessagesRead(data) {
        // Implement UI update for all messages read
    }

    onUserOnline(userId) {
        // Implement UI update for user online status
    }

    onUserOffline(userId) {
        // Implement UI update for user offline status
    }

    onUserStartedTyping(userId, conversationId) {
        // Implement typing indicator UI
    }

    onUserStoppedTyping(userId, conversationId) {
        // Remove typing indicator UI
    }

    onError(error) {
        // Handle errors
    }

    onConversationCreated(conversation) {
        // Handle new conversation
    }

    onOnlineUsersReceived(users) {
        // Update online users list
    }
}

// Usage example
/*
const chatClient = new ChatClient("https://localhost:6005/chatHub", "your-jwt-token");

// Connect
await chatClient.connect();

// Join a conversation
await chatClient.joinConversation("conversation-id");

// Send a message
await chatClient.sendMessage("conversation-id", "Hello world!");

// Handle received messages
chatClient.onMessageReceived = (message) => {
    // Update your UI with the new message
    console.log("New message:", message);
};

// Handle typing indicators
chatClient.onUserStartedTyping = (userId, conversationId) => {
    // Show typing indicator for user
};

chatClient.onUserStoppedTyping = (userId, conversationId) => {
    // Hide typing indicator for user
};
*/

export default ChatClient;